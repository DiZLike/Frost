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

            if (_useReplayGain)
            {
                SetupCompressor();
            }
        }

        private void SetupCompressor()
        {
            // Создаем эффект компрессора для Replay Gain
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                1
            );

            _compressor = new BASS_BFX_COMPRESSOR2
            {
                fAttack = 0.01f,
                fRelease = 250f,
                fThreshold = 0f,
                fRatio = 100f
            };
        }

        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_useReplayGain)
                return;

            if (_useCustomGain && !string.IsNullOrEmpty(tagInfo.comment))
            {
                // Пытаемся извлечь кастомный gain из комментария
                _compressor.fGain = ExtractCustomGain(tagInfo.comment);
            }
            else
            {
                // Используем Replay Gain из тегов
                _compressor.fGain = tagInfo.replaygain_track_gain;
            }
        }

        private float ExtractCustomGain(string comment)
        {
            // Ищем gain= значение в комментарии
            // Например: "custom_gain=-2.5dB"
            const string gainMarker = "gain=";

            int startIndex = comment.IndexOf(gainMarker, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return 0f;

            startIndex += gainMarker.Length;
            int endIndex = comment.IndexOf("dB", startIndex, StringComparison.OrdinalIgnoreCase);

            if (endIndex == -1)
                return 0f;

            string gainValue = comment.Substring(startIndex, endIndex - startIndex).Trim();

            if (float.TryParse(gainValue, out float result))
                return result;

            return 0f;
        }

        public void ApplyGain()
        {
            if (!_useReplayGain || _fxHandle == 0)
                return;

            Bass.BASS_FXSetParameters(_fxHandle, _compressor);
        }
    }
}