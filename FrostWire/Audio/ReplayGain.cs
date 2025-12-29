using Strimer.Core;
using System.Globalization;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class ReplayGain
    {
        private readonly bool _useReplayGain;           // Включен ли ReplayGain
        private readonly bool _useCustomGain;           // Использовать кастомное усиление
        private readonly int _mixerHandle;              // Хэндл микшера Bass
        private int _fxHandle;                          // Хэндл эффекта компрессора
        private readonly BASS_BFX_COMPRESSOR2 _compressor; // Настройки компрессора
        private string _gainSource = String.Empty;      // Источник текущего усиления (для логов)
        private string _fileName = String.Empty;

        public ReplayGain(bool useReplayGain, bool useCustomGain, int mixerHandle)
        {
            _useReplayGain = useReplayGain;
            _useCustomGain = useCustomGain;
            _mixerHandle = mixerHandle;

            Logger.Debug($"[ReplayGain] ReplayGain инициализирован: UseReplayGain={useReplayGain}, UseCustomGain={useCustomGain}");

            // Инициализация компрессора один раз (настройки постоянные)
            _compressor = new BASS_BFX_COMPRESSOR2
            {
                fAttack = 0.01f,    // Атака 0.01 секунды
                fRelease = 100f,    // Релиз 100 секунд
                fThreshold = -3f,   // Порог -3 дБ
                fRatio = 100f,      // Коэффициент 100:1
                fGain = 0f          // Начальное усиление 0 дБ
            };

            if (_useReplayGain)
            {
                SetupCompressor();  // Настраиваем компрессор если включен
            }
        }

        private void SetupCompressor()
        {
            // Эта проверка может быть важна для инициализации BassFx
            int version = BassFx.BASS_FX_GetVersion();
            Logger.Debug($"[ReplayGain] BassFx версия: {version}");

            // Удаляем старый эффект если был
            if (_fxHandle != 0)
                Bass.BASS_ChannelRemoveFX(_mixerHandle, _fxHandle);

            // Создаем эффект компрессора для Replay Gain
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                1  // Приоритет 1
            );

            if (_fxHandle == 0)  // Проверяем успешность создания
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[ReplayGain] Не удалось создать компрессор ReplayGain: {error}");
                return;
            }

            Logger.Debug($"[ReplayGain] Компрессор ReplayGain создан (handle: {_fxHandle})");
        }

        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_useReplayGain)  // Если ReplayGain выключен
            {
                Logger.Info("[ReplayGain] ReplayGain отключен, пропускаем регулировку усиления");
                return;
            }

            _fileName = tagInfo.filename;
            float gainValue = 0f;
            _gainSource = "отсутствует";

            // 1. Пробуем кастомное усиление из комментария (если включено)
            if (_useCustomGain)
            {
                gainValue = ExtractCustomGain(tagInfo.comment);
                if (Math.Abs(gainValue) > 0.001f)  // Если нашли кастомное усиление
                {
                    _gainSource = "кастомный комментарий";
                    ApplyGainValue(gainValue);  // Применяем и выходим
                    return;
                }
                else if (!string.IsNullOrEmpty(tagInfo.comment))
                {
                    Logger.Debug($"[ReplayGain] Кастомное усиление не найдено в комментарии: '{tagInfo.comment}'");
                }
            }

            // 2. Пробуем ReplayGain из тегов
            float tagGain = tagInfo.replaygain_track_gain;

            // Проверяем на разумные пределы и игнорируем специальные значения
            if (Math.Abs(tagGain) > 0.001f &&        // Не ноль
                Math.Abs(tagGain - 100f) > 0.01f &&  // Не специальное значение (100)
                tagGain >= -24f &&                   // Не меньше -24 дБ
                tagGain <= 24f)                      // Не больше +24 дБ
            {
                gainValue = tagGain;
                _gainSource = "тег трека";
                Logger.Info($"[ReplayGain] Используется ReplayGain из тегов: {gainValue:F2} дБ");
                ApplyGainValue(gainValue);
                return;
            }
            else if (Math.Abs(tagGain) > 0.001f)  // Есть значение, но отфильтровано
            {
                Logger.Info($"[ReplayGain] Трек имеет ReplayGain {tagGain:F2} дБ, но значение отфильтровано");
            }

            // 3. Если ничего не найдено
            Logger.Info("[ReplayGain] Данные ReplayGain не найдены, используется 0 дБ");
            _gainSource = "по умолчанию (0 дБ)";
            ApplyGainValue(0f);
        }

        private float ExtractCustomGain(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return 0f;

            string lowerComment = comment.ToLowerInvariant();

            // Ищем оба формата: "replay-gain=" и "gain="
            int markerIndex = lowerComment.IndexOf("replay-gain=");
            if (markerIndex == -1)
                markerIndex = lowerComment.IndexOf("gain=");

            if (markerIndex == -1)
                return 0f;  // Маркер не найден

            // Находим начало числа
            int startIndex = markerIndex + (lowerComment.Contains("replay-gain=") ? "replay-gain=".Length : "gain=".Length);

            // Ищем конец числа (до первого нечислового символа кроме знаков и точки/запятой)
            int endIndex = startIndex;
            while (endIndex < comment.Length &&
                   (char.IsDigit(comment[endIndex]) ||
                    comment[endIndex] == '.' ||
                    comment[endIndex] == ',' ||
                    comment[endIndex] == '-' ||
                    comment[endIndex] == '+'))
            {
                endIndex++;
            }

            if (endIndex <= startIndex)  // Не нашли число
                return 0f;

            // Извлекаем и парсим число
            string gainStr = comment.Substring(startIndex, endIndex - startIndex)
                .Replace(',', '.');  // Заменяем запятую на точку для парсинга

            if (float.TryParse(gainStr, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float result))
            {
                return result;  // Ограничение диапазона будет в SetGain
            }

            return 0f;  // Ошибка парсинга
        }

        private void ApplyGainValue(float gainValue)
        {
            // Ограничиваем значение для безопасности
            gainValue = Math.Max(-24f, Math.Min(24f, gainValue));

            // Устанавливаем усиление в компрессор
            _compressor.fGain = gainValue;

            // Применяем сразу
            ApplyGain();
        }

        public void ApplyGain()
        {
            if (!_useReplayGain || _fxHandle == 0)  // Проверяем доступность
            {
                Logger.Warning("[ReplayGain] Не удалось применить усиление: ReplayGain отключен или отсутствует FX handle");
                return;
            }

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressor);
            if (success)
            {
                Logger.Info($"[ReplayGain] ReplayGain применен к {Path.GetFileName(_fileName)}: {_compressor.fGain:F2} дБ (источник: {_gainSource})");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[ReplayGain] Не удалось применить ReplayGain: {error}");
            }
        }

        public void Reset()
        {
            _compressor.fGain = 0f;          // Сбрасываем усиление на 0
            _gainSource = "сброс (0 дБ)";    // Обновляем источник

            if (_fxHandle != 0)              // Применяем если есть хэндл
            {
                Bass.BASS_FXSetParameters(_fxHandle, _compressor);
                Logger.Info("[ReplayGain] ReplayGain сброшен к 0 дБ");
            }
        }
    }
}