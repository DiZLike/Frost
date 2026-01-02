using FrostWire.Core;
using FrostWire.Models;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace FrostWire.Audio.FX
{
    public enum CompressionMode
    {
        Soft,
        Medium,
        Hard
    }
    public class Compressor
    {
        private readonly int _mixerHandle;
        private int _fxHandle;
        private readonly BASS_BFX_COMPRESSOR2 _compressorParams;
        private bool _enabled = true;
        private bool _initialized = false;

        // Настройки компрессора для музыкального контента
        private readonly CompressorModel _parameters;

        public Compressor(int mixerHandle, CompressorModel parameters)
        {
            _mixerHandle = mixerHandle;
            _parameters = parameters;

            // Настройки компрессора для музыкального контента
            _compressorParams = new BASS_BFX_COMPRESSOR2
            {
                fThreshold = parameters.Threshold,
                fRatio = parameters.Ratio,
                fAttack = parameters.Attack,
                fRelease = parameters.Release,
                fGain = parameters.Gain
            };

            if (Initialize())
                Logger.Debug($"[Compressor] Загружены параметры: T={parameters.Threshold}; R={parameters.Ratio}; " +
                    $"A={parameters.Attack}; R={parameters.Release}, G={parameters.Gain}");
            else
                throw new Exception($"Не удалось инициализировать компрессор: {Bass.BASS_ErrorGetCode()}");
        }

        private bool Initialize()
        {
            if (_initialized) return true;

            // Проверяем доступность BassFX
            int version = BassFx.BASS_FX_GetVersion();
            if (version == 0)
            {
                Logger.Error("[Compressor] BassFX не доступен");
                return false;
            }

            // Создаем эффект компрессора
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                _parameters.Priority
            );

            if (_fxHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Compressor] Не удалось создать эффект: {error}");
                return false;
            }

            // Применяем параметры
            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressorParams);
            if (success)
            {
                _initialized = true;
                Logger.Debug($"[Compressor] Эффект создан успешно (handle: {_fxHandle})");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Compressor] Не удалось установить параметры: {error}");
                return false;
            }
            return true;
        }

        public void SetCompressor(float replayGainValue, float rms)
        {
            if (!_initialized || !_enabled) return;
            if(_parameters.Adaptive)
            {
                //// Нормализуем значение ReplayGain
                //float thresholdOffset = -replayGainValue * 0.3f;

                //// Динамическая адаптация параметров
                //float dynamicThreshold = _parameters.Threshold + thresholdOffset;
                //float dynamicRatio = _parameters.Ratio + (replayGainValue * 0.05f);

                //// Ограничиваем значения
                //dynamicThreshold = Math.Max(-30f, Math.Min(-6f, dynamicThreshold));
                //dynamicRatio = Math.Max(1.5f, Math.Min(15f, dynamicRatio));

                var (threshold, ratio, attackMs, releaseMs, makeupGain) = 
                    CalculateRadioCompressor(rms, replayGainValue, CompressionMode.Medium, false);

                // Обновляем остальные параметры
                UpdateParameters(threshold, ratio, attackMs, releaseMs, makeupGain);

                Logger.Debug($"[Compressor] Адаптировано для ReplayGain {replayGainValue:F1} dB:");
            }
            else
            {
                UpdateParameters(_parameters.Threshold, _parameters.Ratio, _parameters.Attack, _parameters.Release, _parameters.Gain);
                Logger.Debug($"[Compressor] Компрессор установлен");
            }
            
        }

        public enum CompressionMode { Soft, Medium, Hard }

        public (float threshold, float ratio, float attackMs, float releaseMs, float makeupGain)
    CalculateRadioCompressor(float trackRmsDb, float replayGainDb, CompressionMode mode = CompressionMode.Medium, bool isDynamicTrack = true)
        {
            // ============ ЧАСТЬ 1: ИНИЦИАЛИЗАЦИЯ ПАРАМЕТРОВ ПО РЕЖИМУ ============
            float targetRadioRms, baseRatioMultiplier, timeScaleFactor;

            // Ближе к 0 dB = громче, дальше от 0 = тише
            switch (mode)
            {
                case CompressionMode.Soft:
                    targetRadioRms = -17.0f;  // Soft = тише (дальше от 0)
                    baseRatioMultiplier = 0.8f;
                    timeScaleFactor = 1.5f;
                    break;
                case CompressionMode.Medium:
                    targetRadioRms = -16.0f;
                    baseRatioMultiplier = 1.0f;
                    timeScaleFactor = 1.0f;
                    break;
                case CompressionMode.Hard:
                    targetRadioRms = -15.0f;  // Hard = громче (ближе к 0)
                    baseRatioMultiplier = 1.2f;
                    timeScaleFactor = 0.7f;
                    break;
                default:
                    targetRadioRms = -16.0f;
                    baseRatioMultiplier = 1.0f;
                    timeScaleFactor = 1.0f;
                    break;
            }

            // ============ ЧАСТЬ 2: АНАЛИЗ ИСХОДНОЙ ГРОМКОСТИ ============
            float originalRms = trackRmsDb;  // До replayGain
            float normalizedRms = trackRmsDb + replayGainDb;  // После replayGain

            // Определяем насколько оригинал громкий/тихий
            float loudnessFactor = Math.Abs(originalRms);  // Чем меньше число, тем громче

            // Рассчитываем deviation от цели (на основе ОРИГИНАЛЬНОГО RMS)
            float deviationFromTarget = originalRms - targetRadioRms;
            // Положительное = оригинал громче цели, отрицательное = оригинал тише цели

            // ============ ЧАСТЬ 3: РАСЧЕТ ПОРОГА ============
            float threshold;

            // ГРОМКОСТЬ ОРИГИНАЛА определяет порог:
            // Громкие треки (близко к 0) → высокий порог → сжимаем только пики
            // Тихие треки (далеко от 0) → низкий порог → активное сжатие

            if (originalRms >= -10.0f)  // Очень громкие: -10 dB до 0 dB
            {
                Logger.Info($"[Compressor] Значительно громче цели. originalRms: {originalRms}");
                threshold = originalRms + 4.0f;  // Высокий порог
                threshold = Math.Min(threshold, -3.0f);  // Не выше -3 dB
            }
            else if (originalRms >= -14.0f)  // Громкие: -14 до -10 dB
            {
                Logger.Info($"[Compressor] Громче цели. originalRms: {originalRms}");
                threshold = originalRms + 3.0f;
            }
            else if (originalRms >= -18.0f)  // Средние: -18 до -14 dB
            {
                Logger.Info($"[Compressor] Немного тише цели. originalRms: {originalRms}");
                threshold = originalRms + 2.0f;
            }
            else if (originalRms >= -22.0f)  // Тихие: -22 до -18 dB
            {
                Logger.Info($"[Compressor] Тише цели. originalRms: {originalRms}");
                threshold = originalRms + 1.0f;
            }
            else  // Очень тихие: ниже -22 dB
            {
                Logger.Log($"[Compressor] Значительно тише цели. originalRms: {originalRms}");
                threshold = originalRms - 1.0f;  // Низкий порог
                threshold = Math.Max(threshold, -35.0f);  // Не ниже -35 dB
            }
            float tNoRg = threshold;
            threshold += replayGainDb;
            Logger.Log($"[Compressor] Значение threshold: {threshold}; (Без RG: {tNoRg})");

            // ============ ЧАСТЬ 4: РАСЧЕТ КОЭФФИЦИЕНТА СЖАТИЯ ============
            float ratio;

            // Ratio зависит от того, насколько оригинал отличается от цели:
            // 1. Чем громче оригинал (ближе к 0), тем меньше ratio (мягче)
            // 2. Чем тише оригинал, тем больше ratio (агрессивнее)

            if (deviationFromTarget > 3.0f)  // Оригинал ЗНАЧИТЕЛЬНО громче цели
            {
                Logger.Debug($"[Compressor] Значительно громче цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 1.5f * baseRatioMultiplier;  // Мягкое сжатие
            }
            else if (deviationFromTarget > 0)  // Оригинал громче цели
            {
                Logger.Debug($"[Compressor] Громче цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 1.8f * baseRatioMultiplier;
            }
            else if (deviationFromTarget > -3.0f)  // Оригинал немного тише цели
            {
                Logger.Debug($"[Compressor] Немного тише цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 2.5f * baseRatioMultiplier;
            }
            else if (deviationFromTarget > -6.0f)  // Оригинал тише цели
            {
                Logger.Debug($"[Compressor] Тише цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 3.5f * baseRatioMultiplier;
            }
            else if (deviationFromTarget > -9.0f)  // Оригинал значительно тише цели
            {
                Logger.Debug($"[Compressor] Значительно тише цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 5.0f * baseRatioMultiplier;
            }
            else  // Оригинал намного тише цели
            {
                Logger.Debug($"[Compressor] Намного тише цели. deviationFromTarget: {deviationFromTarget}");
                ratio = 6.5f * baseRatioMultiplier;
            }

            ratio = Math.Max(1.1f, Math.Min(ratio, 8.0f));

            // ============ ЧАСТЬ 5: ВРЕМЕННЫЕ ПАРАМЕТРЫ ============
            float attackMs, releaseMs;

            // Времена зависят от ratio и громкости оригинала
            float baseAttack = 40.0f / (ratio * 0.5f);  // Чем выше ratio, тем быстрее атака
            float baseRelease = 400.0f / (ratio * 0.5f);

            // Быстрые времена для громких треков (чтобы контролировать пики)
            if (originalRms >= -12.0f)  // Громкие треки
            {
                baseAttack *= 0.7f;
                baseRelease *= 0.6f;
            }

            if (isDynamicTrack)
            {
                baseRelease *= 1.8f;  // Для динамичных треков медленнее релиз
            }

            attackMs = baseAttack * timeScaleFactor;
            releaseMs = baseRelease * timeScaleFactor;

            attackMs = Math.Max(1.0f, Math.Min(attackMs, 100.0f));
            releaseMs = Math.Max(20.0f, Math.Min(releaseMs, 1000.0f));

            return (threshold, ratio, attackMs, releaseMs, 0);
        }


        // Рабочий вариант
        //public (float threshold, float ratio, float attackMs, float releaseMs, float makeupGain)
        //    CalculateRadioCompressor(float trackRmsDb, float replayGainDb, bool isDynamicTrack = true)
        //{
        //    // ============ ЧАСТЬ 1: НОРМАЛИЗАЦИЯ УРОВНЯ ============
        //    // ReplayGain уже был применён к треку в цепи
        //    float normalizedRms = trackRmsDb + replayGainDb;

        //    // Целевой RMS для радио
        //    const float targetRadioRms = -16.0f;

        //    // ============ ЧАСТЬ 2: РАСЧЁТ ПОРОГА И КОЭФФИЦИЕНТА ============
        //    float threshold;
        //    float ratio;

        //    // Определяем насколько трек отклоняется от цели
        //    float deviation = targetRadioRms - normalizedRms; // положительное = нужно усиливать

        //    if (normalizedRms < targetRadioRms - 6f) // Очень тихий трек (< -22 dB)
        //    {
        //        // Сильное сжатие + низкий порог
        //        threshold = normalizedRms - 3.0f; // Порог ниже RMS для активного сжатия
        //        ratio = 4.0f; // Агрессивное сжатие 4:1
        //    }
        //    else if (normalizedRms < targetRadioRms - 3) // Тихий трек (-19...-22 dB)
        //    {
        //        threshold = normalizedRms - 2.0f;
        //        ratio = 3.0f; // Сильное сжатие 3:1
        //    }
        //    else if (normalizedRms < targetRadioRms) // Чуть тише цели (-16...-19 dB)
        //    {
        //        threshold = normalizedRms - 1.0f;
        //        ratio = 2.5f; // Умеренное сжатие
        //    }
        //    else if (normalizedRms < targetRadioRms + 3) // Немного громче цели (-13...-16 dB)
        //    {
        //        threshold = normalizedRms + 2.0f; // Порог выше RMS
        //        ratio = 2.0f; // Легкое сжатие
        //    }
        //    else if (normalizedRms < targetRadioRms + 6) // Громкий трек (-10...-13 dB)
        //    {
        //        threshold = normalizedRms + 4.0f;
        //        ratio = 1.5f; // Очень легкое сжатие
        //    }
        //    else // Очень громкий трек (> -10 dB)
        //    {
        //        threshold = normalizedRms + 6.0f;
        //        ratio = 1.2f;
        //    }

        //    // ============ ЧАСТЬ 3: ВРЕМЕННЫЕ ПАРАМЕТРЫ ============
        //    // Чем сильнее сжатие, тем быстрее должны быть времена
        //    float attackMs, releaseMs;

        //    if (ratio >= 3.5f)
        //    {
        //        // Агрессивное сжатие - быстрые времена
        //        attackMs = isDynamicTrack ? 15.0f : 5.0f;
        //        releaseMs = isDynamicTrack ? 150.0f : 60.0f;
        //    }
        //    else if (ratio >= 2.5)
        //    {
        //        attackMs = isDynamicTrack ? 20.0f : 10.0f;
        //        releaseMs = isDynamicTrack ? 200.0f : 100.0f;
        //    }
        //    else
        //    {
        //        // Легкое сжатие - можно медленнее
        //        attackMs = isDynamicTrack ? 30.0f : 15.0f;
        //        releaseMs = isDynamicTrack ? 300.0f : 150.0f;
        //    }

        //    // ============ ЧАСТЬ 4: КОМПЕНСАЦИОННОЕ УСИЛЕНИЕ ============
        //    // Makeup gain = насколько нужно поднять уровень после сжатия
        //    float makeupGain = targetRadioRms - normalizedRms;

        //    // Дополнительная компенсация: сильное сжатие снижает пики,
        //    // поэтому можно добавить больше усиления
        //    if (ratio > 3.0f)
        //    {
        //        makeupGain += 2.0f; // +2 dB дополнительно
        //    }
        //    else if (ratio > 2.0f)
        //    {
        //        makeupGain += 1.0f; // +1 dB дополнительно
        //    }

        //    // ============ ВОЗВРАТ РЕЗУЛЬТАТА ============
        //    return (threshold, ratio, attackMs, releaseMs, makeupGain);
        //}

        public void UpdateParameters(float threshold, float ratio, float attack, float release, float gain)
        {
            if (!_initialized || !_enabled) return;

            _compressorParams.fThreshold = threshold;
            _compressorParams.fRatio = Math.Max(1f, Math.Min(100f, ratio));
            _compressorParams.fAttack = Math.Max(0.01f, Math.Min(100f, attack));
            _compressorParams.fGain = gain;

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressorParams);
            if (success)
            {
                Logger.Info($"[Compressor] Параметры обновлены:" +
                            $"\n\t\t\t\tThreshold=   {_compressorParams.fThreshold:F2}dB, " +
                            $"\n\t\t\t\tRatio=  {_compressorParams.fRatio:F2}:1, " +
                            $"\n\t\t\t\tAttack=  {_compressorParams.fAttack:F2}, " +
                            $"\n\t\t\t\tRelease=  {_compressorParams.fRelease:F2}, " +
                            $"\n\t\t\t\tGain=    {_compressorParams.fGain:F2}dB");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Compressor] Не удалось обновить параметры: {error}");
            }
        }

        public void Cleanup()
        {
            if (_fxHandle != 0 && _initialized)
            {
                _initialized = false;
                _fxHandle = 0;  // Сбросить handle
                Logger.Debug("[Compressor] Ресурсы освобождены");
            }
        }
    }
}