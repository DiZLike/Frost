using FrostWire.Core;
using FrostWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace FrostWire.Audio.FX
{
    public class Fader
    {
        private readonly int _mixerHandle;
        private int _fxHandle;
        private readonly BASS_BFX_COMPRESSOR2 _fader;
        private bool _initialized = false;

        private FaderModel _params;

        public event Action? FadeCompleted;

        public Fader(int mixerHandle, FaderModel parameters)
        {
            _mixerHandle = mixerHandle;
            _params = parameters;

            _fader = new BASS_BFX_COMPRESSOR2
            {
                fAttack = 0.01f,
                fRelease = 200,
                fRatio = 1,
                fThreshold = 0,
                fGain = 0
            };
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _fxHandle = Bass.BASS_ChannelSetFX(_mixerHandle, BASSFXType.BASS_FX_BFX_COMPRESSOR2, 0);
            if (_fxHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Fader] Не удалось создать эффект: {error}");
                return;
            }

            // Применяем параметры
            bool success = Bass.BASS_FXSetParameters(_fxHandle, _fader);
            if (success)
            {
                _initialized = true;
                _params.FadeElapsedSeconds = 0;
                _params.FaderTimer.Elapsed += FaderTimer_Elapsed;
                Logger.Debug($"[Fader] Эффект создан успешно (handle: {_fxHandle})");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Fader] Не удалось установить параметры: {error}");
            }
            _fader.fGain = -20f;
        }
        public void FadeStart(double pos, double len)
        {
            if (_params.IsFading) return;
            if (len - pos > _params.Duration) return;
            _params.IsFading = true;
            _params.FaderTimer.Start();
            Logger.Debug($"[Fader] Фейд-аут запущен. Длительность: {_params.Duration} сек");
        }

        private void FaderTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _params.FadeElapsedSeconds += 0.05f;
            if ( _params.FadeElapsedSeconds >= _params.Duration)
            {
                FadeComplete();
                return;
            }
            float progress = _params.FadeElapsedSeconds / _params.Duration;
            float currentVolume = _params.FadeStartVolume + (_params.FadeEndVolume - _params.FadeStartVolume) * progress;
            _fader.fGain = currentVolume;
            Bass.BASS_FXSetParameters(_fxHandle, _fader);

        }
        private void FadeComplete()
        {
            _params.FaderTimer.Stop();
            _params.FadeElapsedSeconds = 0;
            //_fader.fGain = _params.FadeEndVolume;
            _fader.fGain = _params.FadeStartVolume;
            Bass.BASS_FXSetParameters(_fxHandle, _fader);

            _params.IsFading = false;
            Logger.Debug("[Fader] Фейд-аут завершен");
            FadeCompleted?.Invoke();
        }
        public void FadeStop()
        {
            if ( _params.FaderTimer != null)
                _params.FaderTimer.Stop();
            _params.IsFading = false;
        }
    }
}
