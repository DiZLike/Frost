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

            Logger.Info($"ReplayGain initialized: UseReplayGain={useReplayGain}, UseCustomGain={useCustomGain}");

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
                Logger.Error($"Failed to create ReplayGain compressor: {error}");
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

            Logger.Info($"ReplayGain compressor created (handle: {_fxHandle})");
        }

        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_useReplayGain)
            {
                Logger.Info("ReplayGain disabled, skipping gain adjustment");
                return;
            }

            float gainValue = 0f;
            string source = "none";
            bool gainFound = false;

            // 1. Пробуем кастомный gain из комментария (если включено)
            if (_useCustomGain && !string.IsNullOrEmpty(tagInfo.comment))
            {
                gainValue = ExtractCustomGain(tagInfo.comment);

                // Проверяем, действительно ли нашли gain (не 0 и не ошибка парсинга)
                if (Math.Abs(gainValue) > 0.001f)
                {
                    source = "custom comment";
                    gainFound = true;
                    Logger.Info($"Using custom gain from comment: {gainValue:F2} dB");
                }
                else
                {
                    // Gain не найден в комментарии, но это не ошибка
                    Logger.Info($"Custom gain not found in comment: '{tagInfo.comment}'");
                }
            }
            else if (_useCustomGain && string.IsNullOrEmpty(tagInfo.comment))
            {
                Logger.Info("Custom gain enabled but comment is empty");
            }

            // 2. Если кастомный gain не найден ИЛИ не включен, пробуем теги
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
                    source = "track tag";
                    gainFound = true;
                    Logger.Info($"Using ReplayGain from tags: {gainValue:F2} dB");
                }
                else if (Math.Abs(tagGain) > 0.001f)
                {
                    // Есть значение, но оно отфильтровано
                    Logger.Info($"Track has ReplayGain {tagGain:F2} dB but filtered " +
                                $"(range: {-24}..{+24}, special values ignored)");
                }
            }

            // 3. Если ничего не найдено
            if (!gainFound)
            {
                Logger.Info("No valid ReplayGain data found, using 0 dB");
                source = "default (0 dB)";
            }

            // Единое ограничение для безопасности
            gainValue = Math.Max(-24f, Math.Min(24f, gainValue));

            // Обновляем компрессор
            _compressor.fGain = gainValue;

            Logger.Info($"ReplayGain final: {gainValue:F2} dB (source: {source})");
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
                Logger.Warning("Cannot apply gain: ReplayGain disabled or no FX handle");
                return;
            }

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _compressor);

            if (success)
            {
                Logger.Info($"ReplayGain applied: {_compressor.fGain:F2} dB");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"Failed to apply ReplayGain: {error}");
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