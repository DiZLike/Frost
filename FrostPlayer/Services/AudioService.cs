using System;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Opus;

namespace FrostPlayer.Services
{
    public class AudioService : IDisposable
    {
        private int _stream;
        private bool _isInitialized;

        // Для поддержки opus
        private const int BASS_STREAM_PRESCAN = 0x20000;

        public AudioService()
        {
            InitializeBass();
        }

        private void InitializeBass()
        {
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                throw new Exception("Ошибка инициализации BASS!");
            }
            _isInitialized = true;
        }

        public int LoadFile(string filePath)
        {
            if (!_isInitialized) return 0;

            // Определяем флаги в зависимости от формата
            BASSFlag flags = BASSFlag.BASS_DEFAULT;
            if (System.IO.Path.GetExtension(filePath).ToLower() == ".opus")
            {
                flags |= (BASSFlag)BASS_STREAM_PRESCAN;
            }

            _stream = Bass.BASS_StreamCreateFile(filePath, 0, 0, flags);
            return _stream;
        }

        public bool Play()
        {
            if (_stream == 0) return false;
            return Bass.BASS_ChannelPlay(_stream, false);
        }

        public bool Pause()
        {
            if (_stream == 0) return false;
            return Bass.BASS_ChannelPause(_stream);
        }

        public bool Stop()
        {
            if (_stream == 0) return false;

            var result = Bass.BASS_ChannelStop(_stream);
            Bass.BASS_StreamFree(_stream);
            _stream = 0;
            return result;
        }

        public bool SetPosition(double seconds)
        {
            if (_stream == 0) return false;
            var position = Bass.BASS_ChannelSeconds2Bytes(_stream, seconds);
            return Bass.BASS_ChannelSetPosition(_stream, position);
        }

        public double GetPosition()
        {
            if (_stream == 0) return 0;
            var position = Bass.BASS_ChannelGetPosition(_stream);
            return Bass.BASS_ChannelBytes2Seconds(_stream, position);
        }

        public double GetDuration()
        {
            if (_stream == 0) return 0;
            var length = Bass.BASS_ChannelGetLength(_stream);
            return Bass.BASS_ChannelBytes2Seconds(_stream, length);
        }

        public bool SetVolume(float volume)
        {
            if (_stream == 0) return false;
            return Bass.BASS_ChannelSetAttribute(_stream, BASSAttribute.BASS_ATTRIB_VOL, volume);
        }

        public bool IsPlaying()
        {
            if (_stream == 0) return false;
            return Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING;
        }

        public bool IsPaused()
        {
            if (_stream == 0) return false;
            return Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PAUSED;
        }

        public string GetLastError()
        {
            return Bass.BASS_ErrorGetCode().ToString();
        }

        public void Dispose()
        {
            Stop();
            if (_isInitialized)
            {
                Bass.BASS_Free();
                _isInitialized = false;
            }
        }
    }
}