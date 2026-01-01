using FrostWire.Core;
using System.Globalization;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Tags;

namespace FrostWire.Audio.FX
{
    public class ReplayGain
    {
        private readonly bool _useReplayGain;
        private readonly bool _useCustomGain;
        private readonly int _mixerHandle;
        private int _fxHandle;
        private readonly BASS_BFX_COMPRESSOR2 _compressor;
        private string _gainSource = String.Empty;
        private string _fileName = String.Empty;
        private bool _enabled = true;
        private bool _initialized = false;

        public float GainValue { get; set; } = 0f;

        public ReplayGain(bool useReplayGain, bool useCustomGain, int mixerHandle)
        {
            _useReplayGain = useReplayGain;
            _useCustomGain = useCustomGain;
            _mixerHandle = mixerHandle;

            Logger.Debug($"[ReplayGain] Инициализирован: UseReplayGain={useReplayGain}, UseCustomGain={useCustomGain}");

            // Инициализация компрессора для ReplayGain
            _compressor = new BASS_BFX_COMPRESSOR2
            {
                fAttack = 0.01f,
                fRelease = 100f,
                fThreshold = -3f,
                fRatio = 100f,
                fGain = 0f
            };

            if (_useReplayGain)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (_initialized) return;

            int version = BassFx.BASS_FX_GetVersion();
            Logger.Debug($"[ReplayGain] BassFx версия: {version}");

            // Создаем эффект компрессора для Replay Gain
            _fxHandle = Bass.BASS_ChannelSetFX(
                _mixerHandle,
                BASSFXType.BASS_FX_BFX_COMPRESSOR2,
                4
            );

            if (_fxHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[ReplayGain] Не удалось создать компрессор: {error}");
                return;
            }

            _initialized = true;
            Logger.Debug($"[ReplayGain] Компрессор создан (handle: {_fxHandle})");
        }

        public void Disable()
        {
            _enabled = false;
            if (_fxHandle != 0)
            {
                Bass.BASS_ChannelRemoveFX(_mixerHandle, _fxHandle);
                Logger.Debug("[ReplayGain] Эффект отключен");
            }
        }

        public void SetGain(TAG_INFO tagInfo)
        {
            if (!_useReplayGain || !_enabled)
            {
                Logger.Info("[ReplayGain] ReplayGain отключен, пропускаем регулировку усиления");
                return;
            }

            _fileName = tagInfo.filename;
            GainValue = 0f;
            _gainSource = "отсутствует";

            // 1. Пробуем кастомное усиление из комментария (если включено)
            if (_useCustomGain)
            {
                GainValue = ExtractCustomGain(tagInfo.comment);
                if (Math.Abs(GainValue) > 0.001f)
                {
                    _gainSource = "кастомный комментарий";
                    ApplyGainValue(GainValue);
                    return;
                }
                else if (!string.IsNullOrEmpty(tagInfo.comment))
                {
                    Logger.Debug($"[ReplayGain] Кастомное усиление не найдено в комментарии: '{tagInfo.comment}'");
                }
            }

            // 2. Пробуем ReplayGain из тегов
            float tagGain = tagInfo.replaygain_track_gain;

            if (Math.Abs(tagGain) > 0.001f &&
                Math.Abs(tagGain - 100f) > 0.01f &&
                tagGain >= -24f &&
                tagGain <= 24f)
            {
                GainValue = tagGain;
                _gainSource = "тег трека";
                Logger.Info($"[ReplayGain] Используется ReplayGain из тегов: {GainValue:F2} дБ");
                ApplyGainValue(GainValue);
                return;
            }
            else if (Math.Abs(tagGain) > 0.001f)
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

            int markerIndex = lowerComment.IndexOf("replay-gain=");
            if (markerIndex == -1)
                markerIndex = lowerComment.IndexOf("gain=");

            if (markerIndex == -1)
                return 0f;

            int startIndex = markerIndex + (lowerComment.Contains("replay-gain=") ? "replay-gain=".Length : "gain=".Length);

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

            if (endIndex <= startIndex)
                return 0f;

            string gainStr = comment.Substring(startIndex, endIndex - startIndex)
                .Replace(',', '.');

            if (float.TryParse(gainStr, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }

            return 0f;
        }

        private void ApplyGainValue(float gainValue)
        {
            gainValue = Math.Max(-24f, Math.Min(24f, gainValue));
            _compressor.fGain = gainValue;
            ApplyGain();
        }

        public void ApplyGain()
        {
            if (!_useReplayGain || !_enabled || _fxHandle == 0)
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
            _compressor.fGain = 0f;
            _gainSource = "сброс (0 дБ)";

            if (_fxHandle != 0 && _enabled)
            {
                Bass.BASS_FXSetParameters(_fxHandle, _compressor);
                Logger.Info("[ReplayGain] ReplayGain сброшен к 0 дБ");
            }
        }

        public void Cleanup()
        {
            if (_fxHandle != 0)
            {
                Disable();
                _initialized = false;
                Logger.Debug("[ReplayGain] Ресурсы освобождены");
            }
        }
    }
}