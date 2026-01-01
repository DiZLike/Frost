using FrostWire.Core;
using FrostWire.Models;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace FrostWire.Audio.FX
{
    public class Limiter
    {
        private readonly int _mixerHandle;
        private int _fxHandle;
        private readonly BASS_BFX_COMPRESSOR2 _limiterParams;
        private bool _enabled = true;
        private bool _initialized = false;

        private LimiterModel _parameters;

        public Limiter(int mixerHandle, LimiterModel parameters)
        {
            _mixerHandle = mixerHandle;
            _parameters = parameters;

            // Настройки лимитера (жесткий ограничитель)
            _limiterParams = new BASS_BFX_COMPRESSOR2
            {
                fThreshold = parameters.Threshold,
                fRatio = 100,
                fAttack = 0.01f,
                fRelease = parameters.Release,
                fGain = parameters.Gain
            };

            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            // Создаем эффект лимитера
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                1
            );

            if (_fxHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Limiter] Не удалось создать эффект: {error}");
                return;
            }

            // Применяем параметры
            bool success = Bass.BASS_FXSetParameters(_fxHandle, _limiterParams);
            if (success)
            {
                _initialized = true;
                Logger.Debug($"[Limiter] Эффект создан успешно (handle: {_fxHandle})");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Limiter] Не удалось установить параметры: {error}");
            }
        }
        public void SetLimiter()
        {
            UpdateParameters(_parameters.Threshold, _parameters.Release, _parameters.Gain);
        }
        private void UpdateParameters(float threshold, float release, float gain)
        {
            if (!_initialized || !_enabled) return;

            _limiterParams.fThreshold = threshold;
            _limiterParams.fRelease = release;
            _limiterParams.fGain = gain;

            bool success = Bass.BASS_FXSetParameters(_fxHandle, _limiterParams);
            if (success)
            {
                Logger.Debug($"[Limiter] Параметры обновлены:");
                Logger.Debug($"[Limiter] T={_limiterParams.fThreshold}dB, R={_limiterParams.fRatio}:1 REL={_limiterParams.fRelease}, G={_limiterParams.fGain}db");
            }
            else
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[Limiter] Не удалось обновить параметры: {error}");
            }
        }

        public void Cleanup()
        {
            if (_fxHandle != 0)
            {
                _initialized = false;
                Logger.Debug("[Limiter] Ресурсы освобождены");
            }
        }
    }
}