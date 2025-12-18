using Strimer.Core;
using System.Globalization;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class ReplayGain
    {
        private bool _useReplayGain;
        private bool _useCustomGain;
        private int _mixerHandle;
        private int _fxHandle;
        private BASS_BFX_COMPRESSOR2 _compressor;

        public ReplayGain(bool useReplayGain, bool useCustomGain, int mixerHandle)
        {
            _useReplayGain = useReplayGain;
            _useCustomGain = useCustomGain;
            _mixerHandle = mixerHandle;

            Logger.Info($"ReplayGain инициализирован: UseReplayGain={useReplayGain}, UseCustomGain={useCustomGain}");

            if (_useReplayGain)
            {
                SetupCompressor();
            }
        }

        private void SetupCompressor()
        {
            int version = BassFx.BASS_FX_GetVersion();
            Logger.Info($"BassFx: {version}");
            // Удаляем старый эффект если есть
            if (_fxHandle != 0)
            {
                Bass.BASS_ChannelRemoveFX(_mixerHandle, _fxHandle);
            }

            // Создаем эффект компрессора для Replay Gain
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                1
            );

            if (_fxHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"Не удалось создать компрессор ReplayGain: {error}");
                return;
            }

            _compressor = new BASS_BFX_COMPRESSOR2
            {
                fAttack = 0.01f,
                fRelease = 100f,
                fThreshold = -3f,
                fRatio = 100f,
                fGain = 0f  // Начальное значение
            };

            Logger.Info($"Компрессор ReplayGain создан (handle: {_fxHandle})");
        }

        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_useReplayGain)
            {
                Logger.Info("ReplayGain отключен, пропускаем регулировку усиления");
                return;
            }

            float gainValue = 0f;
            string source = "отсутствует";
            bool gainFound = false;

            // 1. Пробуем кастомное усиление из комментария (если включено)
            if (_useCustomGain && !string.IsNullOrEmpty(tagInfo.comment))
            {
                gainValue = ExtractCustomGain(tagInfo.comment);

                // Проверяем, действительно ли нашли усиление (не 0 и не ошибка парсинга)
                if (Math.Abs(gainValue) > 0.001f)
                {
                    source = "кастомный комментарий";
                    gainFound = true;
                    Logger.Info($"Используется кастомное усиление из комментария: {gainValue:F2} дБ");
                }
                else
                {
                    // Усиление не найдено в комментарии, но это не ошибка
                    Logger.Info($"Кастомное усиление не найдено в комментарии: '{tagInfo.comment}'");
                }
            }
            else if (_useCustomGain && string.IsNullOrEmpty(tagInfo.comment))
            {
                Logger.Info("Кастомное усиление включено, но комментарий пуст");
            }

            // 2. Если кастомное усиление не найдено ИЛИ не включено, пробуем теги
            if (!gainFound)
            {
                float tagGain = tagInfo.replaygain_track_gain;

                // Проверяем на разумные пределы и игнорируем специальные значения
                if (Math.Abs(tagGain) > 0.001f &&
                    Math.Abs(tagGain - 100f) > 0.01f &&
                    tagGain >= -24f &&
                    tagGain <= 24f)
                {
                    gainValue = tagGain;
                    source = "тег трека";
                    gainFound = true;
                    Logger.Info($"Используется ReplayGain из тегов: {gainValue:F2} дБ");
                }
                else if (Math.Abs(tagGain) > 0.001f)
                {
                    // Есть значение, но оно отфильтровано
                    Logger.Info($"Трек имеет ReplayGain {tagGain:F2} дБ, но значение отфильтровано " +
                                $"(диапазон: {-24}..{+24}, специальные значения игнорируются)");
                }
            }

            // 3. Если ничего не найдено
            if (!gainFound)
            {
                Logger.Info("Данные ReplayGain не найдены, используется 0 дБ");
                source = "по умолчанию (0 дБ)";
            }

            // Единое ограничение для безопасности
            gainValue = Math.Max(-24f, Math.Min(24f, gainValue));

            // Обновляем компрессор
            _compressor.fGain = gainValue;

            Logger.Info($"ReplayGain финальное: {gainValue:F2} дБ (источник: {source})");
        }

        private float ExtractCustomGain(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return 0f;

            // Ищем оба формата: "gain=" и "replay-gain=" в нижнем регистре
            string[] gainMarkers = { "replay-gain=", "gain=" };
            string lowerComment = comment.ToLowerInvariant();

            foreach (var marker in gainMarkers)
            {
                int markerIndex = lowerComment.IndexOf(marker);
                if (markerIndex == -1)
                    continue;

                int startIndex = markerIndex + marker.Length;

                // Ищем конец числа (цифры, точка, запятая, минус, плюс)
                int endIndex = startIndex;
                while (endIndex < comment.Length)
                {
                    char c = comment[endIndex];
                    if (char.IsDigit(c) || c == '.' || c == ',' || c == '-' || c == '+')
                    {
                        endIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (endIndex > startIndex)
                {
                    string gainStr = comment.Substring(startIndex, endIndex - startIndex)
                        .Replace(',', '.'); // Заменяем запятую на точку

                    if (float.TryParse(gainStr, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float result))
                    {
                        // Согласовано с SetGain: -24..+24 dB
                        return Math.Max(-24f, Math.Min(24f, result));
                    }
                }

                // Если нашли маркер, но не смогли распарсить, прекращаем поиск
                // (чтобы не искать второй маркер в той же строке)
                break;
            }

            return 0f;
        }

        public void ApplyGain()
        {
            if (!_useReplayGain || _fxHandle == 0)
            {
                Logger.Warning("Не удалось применить усиление: ReplayGain отключен или отсутствует FX handle");
                return;
            }

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressor);

            if (success)
            {
                Logger.Info($"ReplayGain применено: {_compressor.fGain:F2} дБ");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"Не удалось применить ReplayGain: {error}");
            }
        }

        public void Reset()
        {
            _compressor.fGain = 0f;
            if (_fxHandle != 0)
            {
                Bass.BASS_FXSetParameters(_fxHandle, _compressor);
            }
        }
    }
}