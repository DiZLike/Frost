using FrostWire.Core;
using FrostWire.Models;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace FrostWire.Audio.FX
{
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

        public void SetCompressor(float replayGainValue)
        {
            if (!_initialized || !_enabled) return;
            if(_parameters.Adaptive)
            {
                // Нормализуем значение ReplayGain
                float thresholdOffset = -replayGainValue * 0.3f;

                // Динамическая адаптация параметров
                float dynamicThreshold = _parameters.Threshold + thresholdOffset;
                float dynamicRatio = _parameters.Ratio + (replayGainValue * 0.05f);

                // Ограничиваем значения
                dynamicThreshold = Math.Max(-30f, Math.Min(-6f, dynamicThreshold));
                dynamicRatio = Math.Max(1.5f, Math.Min(15f, dynamicRatio));

                // Обновляем остальные параметры
                UpdateParameters(dynamicThreshold, dynamicRatio, _parameters.Gain);

                Logger.Debug($"[Compressor] Адаптировано для ReplayGain {replayGainValue:F1} dB:");
            }
            else
            {
                // Пересчитываем и применяем компенсационное усиление
                UpdateParameters(_parameters.Threshold, _parameters.Ratio, _parameters.Gain);
                Logger.Debug($"[Compressor] Компрессор установлен");
            }
            
        }

        public void UpdateParameters(float? threshold = null, float? ratio = null, float? gain = null)
        {
            if (!_initialized || !_enabled) return;

            if (threshold.HasValue) _compressorParams.fThreshold = Math.Max(-40f, Math.Min(0f, threshold.Value));
            if (ratio.HasValue) _compressorParams.fRatio = Math.Max(1f, Math.Min(100f, ratio.Value));
            if (gain.HasValue)
                _compressorParams.fGain = Math.Max(-12f, Math.Min(12f, gain.Value));

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressorParams);
            if (success)
            {
                Logger.Info($"[Compressor] Параметры обновлены:");
                Logger.Info($"[Compressor]   T={_compressorParams.fThreshold:F1}dB, " +
                            $"R={_compressorParams.fRatio:F1}:1, " +
                            $"G={_compressorParams.fGain:F1}dB [{gain}db]");
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