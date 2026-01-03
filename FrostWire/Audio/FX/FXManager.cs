using FrostWire.App;
using FrostWire.Core;
using FrostWire.Models;
using Un4seen.Bass.AddOn.Tags;

namespace FrostWire.Audio.FX
{
    public class FXManager
    {
        private readonly AppConfig _config;
        private readonly ReplayGain _replayGain;
        private readonly Compressor _firstCompressor;
        private readonly Compressor _secondCompressor;
        private readonly Limiter _limiter;
        private readonly Fader _fader;
        private bool _enabled = true;
        public Limiter Limiter => _limiter;
        public FXManager(int mixerHandle, AppConfig config)
        {
            _config = config;
            // Создаем эффекты в порядке обработки: ReplayGain -> Compressor -> Limiter
            _replayGain = new ReplayGain(config.ReplayGain.UseReplayGain, config.ReplayGain.UseCustomGain, mixerHandle);
            if (config.FirstCompressor.Enable)
            {
                var cparams = new CompressorModel()
                {
                    Adaptive = _config.FirstCompressor.Adaptive,
                    Threshold = _config.FirstCompressor.Threshold,
                    Ratio = _config.FirstCompressor.Ratio,
                    Attack = _config.FirstCompressor.Attack,
                    Release = _config.FirstCompressor.Release,
                    Gain = _config.FirstCompressor.Gain,
                    Priority = 3
                };
                _firstCompressor = new Compressor(mixerHandle, cparams);
                Logger.Info("Первый компрессов включен");
            }
            if (config.SecondCompressor.Enable)
            {
                var cparams = new CompressorModel()
                {
                    Threshold = _config.SecondCompressor.Threshold,
                    Ratio = _config.SecondCompressor.Ratio,
                    Attack = _config.SecondCompressor.Attack,
                    Release = _config.SecondCompressor.Release,
                    Gain = _config.SecondCompressor.Gain,
                    Priority = 2
                };
                _secondCompressor = new Compressor(mixerHandle, cparams);
                Logger.Info("Второй компрессов включен");
            }
            if (config.Limiter.Enable)
            {
                var cparams = new LimiterModel()
                {
                    Threshold = _config.Limiter.Threshold,
                    Release = _config.Limiter.Release,
                    Gain = _config.Limiter.Gain,
                };
                _limiter = new Limiter(mixerHandle, cparams);
                Logger.Info("Лимитер включен");
            }

            var fparams = new FaderModel() { Duration = 3 };
            _fader = new Fader(mixerHandle, fparams);
            Logger.Info("Фейдер включен");

            Logger.Debug($"[FXManager] Инициализирован с mixerHandle={mixerHandle}");
        }
        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_enabled) return;

            // Сначала устанавливаем ReplayGain
            _replayGain?.SetGain(tagInfo);

            // Затем адаптируем компрессор на основе значения ReplayGain
            if (_firstCompressor != null)
            {
                float replayGainValue = _replayGain.GainValue;
                float rms = _replayGain.RMSValue;
                _firstCompressor.SetCompressor(replayGainValue, rms);
            }
            _secondCompressor?.SetCompressor(0, 0);
            _limiter?.SetLimiter();
            
        }
        public void FadeStart(double pos, double len)
        {
            _fader?.FadeStart(pos, len);
        }
        public void FadeStop()
        {
            _fader?.FadeStop();
        }
        public void Cleanup()
        {
            _replayGain?.Cleanup();
            _firstCompressor?.Cleanup();
            _secondCompressor?.Cleanup();
            _limiter?.Cleanup();
            Logger.Debug("[FXManager] Все ресурсы эффектов освобождены");
        }
    }
}