using Strimer.Core;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Strimer.Audio
{
    public class Mixer
    {
        private int _handle;
        private List<int> _streams = new();

        public int Handle => _handle;

        public Mixer(int sampleRate)
        {
            // Убрали неправильную проверку
            // BASS должен быть уже инициализирован к этому моменту

            // Создаем микшерный поток
            _handle = BassMix.BASS_Mixer_StreamCreate(
                sampleRate,
                2, // стерео
                BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT
            );

            if (_handle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception($"Failed to create mixer. Error: {error}");
            }

            Logger.Info($"Mixer created (handle: {_handle})");
        }

        public void AddStream(int stream)
        {
            if (_streams.Contains(stream))
                return;

            bool success = BassMix.BASS_Mixer_StreamAddChannel(
                _handle,
                stream,
                BASSFlag.BASS_MIXER_CHAN_NORAMPIN
            );

            if (success)
            {
                _streams.Add(stream);
                Logger.Info($"Stream {stream} added to mixer");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception($"Failed to add stream to mixer: {error}");
            }
        }

        public void RemoveStream(int stream)
        {
            if (_streams.Contains(stream))
            {
                BassMix.BASS_Mixer_ChannelRemove(stream);
                _streams.Remove(stream);
                Logger.Info($"Stream {stream} removed from mixer");
            }
        }

        public void Clear()
        {
            foreach (var stream in _streams)
            {
                BassMix.BASS_Mixer_ChannelRemove(stream);
            }

            _streams.Clear();
            Logger.Info("Mixer cleared");
        }
    }
}