using Strimer.Audio;
using Strimer.Broadcast.Encoders;
using Strimer.Core;
using System.Net;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;

namespace Strimer.Broadcast
{
    public class IceCastClient : IDisposable
    {
        private readonly AppConfig _config;
        private OpusEncoder _encoder;
        private Mixer _mixer;

        public bool IsConnected { get; private set; }
        public int Listeners { get; private set; }
        public int PeakListeners { get; private set; }

        public IceCastClient(AppConfig config)
        {
            _config = config;
        }

        public void Initialize(Mixer mixer)
        {
            _mixer = mixer;

            Logger.Info("Initializing IceCast client...");

            // Создаем энкодер
            _encoder = new OpusEncoder(_config, _mixer);

            // Подключаемся к IceCast серверу
            ConnectToIceCast();

            Logger.Info("IceCast client initialized");
        }

        private void ConnectToIceCast()
        {
            try
            {
                string url = $"http://{_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}";
                string auth = $"{_config.IceCastUser}:{_config.IceCastPassword}";

                // Инициализируем трансляцию через BASS Encoder
                bool success = BassEnc.BASS_Encode_CastInit(
                    _encoder.Handle,
                    url,
                    auth,
                    "audio/ogg",
                    _config.IceCastName,
                    _config.IceCastGenre,
                    null, null, null,
                    _config.OpusBitrate,
                    BASSEncodeCast.BASS_ENCODE_CAST_PUT
                );

                if (!success)
                {
                    var error = Bass.BASS_ErrorGetCode();
                    throw new Exception($"Failed to initialize IceCast stream: {error}");
                }

                IsConnected = true;
                Logger.Info($"Connected to IceCast: {url}");

                // Запускаем обновление статистики
                StartStatsMonitoring();
            }
            catch (Exception ex)
            {
                Logger.Error($"IceCast connection failed: {ex.Message}");
                IsConnected = false;
            }
        }

        private void StartStatsMonitoring()
        {
            // Запускаем поток для мониторинга статистики
            Thread statsThread = new Thread(MonitorStats);
            statsThread.IsBackground = true;
            statsThread.Start();
        }

        private void MonitorStats()
        {
            while (IsConnected)
            {
                try
                {
                    UpdateListenerStats();
                    Thread.Sleep(10000); // Обновляем каждые 10 секунд
                }
                catch
                {
                    // Игнорируем ошибки в мониторинге
                }
            }
        }

        private void UpdateListenerStats()
        {
            try
            {
                string statsUrl = $"http://{_config.IceCastServer}:{_config.IceCastPort}/status-json.xsl";

                using (var client = new WebClient())
                {
                    // Синхронное скачивание данных
                    string json = client.DownloadString(statsUrl);

                    // Парсим JSON для получения количества слушателей
                    string mountPoint = $"/{_config.IceCastMount}";
                    if (json.Contains(mountPoint))
                    {
                        // Ищем количество слушателей
                        int listenersStart = json.IndexOf("\"listeners\":", StringComparison.Ordinal);
                        if (listenersStart != -1)
                        {
                            listenersStart += 12;
                            int listenersEnd = json.IndexOf(",", listenersStart, StringComparison.Ordinal);
                            if (listenersEnd != -1)
                            {
                                string listenersStr = json.Substring(listenersStart, listenersEnd - listenersStart);
                                if (int.TryParse(listenersStr, out int currentListeners))
                                {
                                    Listeners = currentListeners;

                                    if (currentListeners > PeakListeners)
                                        PeakListeners = currentListeners;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Не падаем если не можем получить статистику
            }
        }

        public void SetMetadata(string artist, string title)
        {
            if (!IsConnected || _encoder == null)
                return;

            _encoder.SetMetadata(artist, title);
        }

        public void Dispose()
        {
            IsConnected = false;

            _encoder?.Dispose();
            _encoder = null;

            Logger.Info("IceCast client disposed");
        }
    }
}