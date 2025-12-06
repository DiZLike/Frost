using Strimer.Core;
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
                fRelease = 250f,
                fThreshold = 0f,
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

            if (_useCustomGain && !string.IsNullOrEmpty(tagInfo.comment))
            {
                // Пытаемся извлечь кастомный gain из комментария
                gainValue = ExtractCustomGain(tagInfo.comment);
                Logger.Info($"Using custom gain from comment: {gainValue} dB");
            }
            else if (Math.Abs(tagInfo.replaygain_track_gain) > 0.001f && Math.Abs(tagInfo.replaygain_track_gain) != 100)
            {
                // Используем Replay Gain из тегов (если значение не нулевое)
                gainValue = tagInfo.replaygain_track_gain;
                Logger.Info($"Using ReplayGain from tags: {gainValue:F2} dB");
            }
            else
            {
                Logger.Info("No ReplayGain data found, using 0 dB");
            }

            _compressor.fGain = gainValue;
            //_compressor.fGain = -40;

            // Логи для отладки
            Logger.Info($"Set compressor gain to: {gainValue:F2} dB");
        }

        private float ExtractCustomGain(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return 0f;

            // Ищем gain= значение в комментарии
            // Форматы: "gain=-2.5dB", "gain=+3.0 dB", "gain=0.0dB"
            const string gainMarker = "gain=";

            int startIndex = comment.IndexOf(gainMarker, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
            {
                Logger.Warning($"No 'gain=' marker found in comment: {comment}");
                return 0f;
            }

            startIndex += gainMarker.Length;

            // Ищем конец значения (пробел, точка с запятой, конец строки)
            int endIndex = comment.Length;
            for (int i = startIndex; i < comment.Length; i++)
            {
                if (comment[i] == ' ' || comment[i] == ';' || comment[i] == ',' ||
                    comment[i] == '\n' || comment[i] == '\r' ||
                    (comment[i] == 'd' && i + 1 < comment.Length && comment[i + 1] == 'B') ||
                    (comment[i] == 'D' && i + 1 < comment.Length && comment[i + 1] == 'b'))
                {
                    endIndex = i;
                    break;
                }
            }

            string gainValue = comment.Substring(startIndex, endIndex - startIndex)
                .Replace(" ", "")
                .Trim();

            if (float.TryParse(gainValue, out float result))
            {
                return result;
            }

            Logger.Warning($"Failed to parse gain value: '{gainValue}' from comment: {comment}");
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