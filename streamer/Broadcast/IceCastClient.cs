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
        private Thread _reconnectThread;
        private bool _isReconnecting;
        private bool _disposed;

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
                if (_isReconnecting)
                {
                    Logger.Info("Reconnection in progress...");
                }

                string url = $"http://{_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}";
                string auth = $"{_config.IceCastUser}:{_config.IceCastPassword}";

                Logger.Info($"Connecting to IceCast: {url}");

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

                    if (error == BASSError.BASS_ERROR_BUSY)
                    {
                        Logger.Warning($"IceCast connection BUSY. Previous connection may not have closed properly.");

                        // Пробуем очистить и переподключиться
                        HandleBusyError();
                        return;
                    }

                    throw new Exception($"Failed to initialize IceCast stream: {error}");
                }

                IsConnected = true;
                _isReconnecting = false;

                Logger.Info($"✓ Connected to IceCast: {url}");

                // Запускаем обновление статистики
                StartStatsMonitoring();
            }
            catch (Exception ex)
            {
                Logger.Error($"IceCast connection failed: {ex.Message}");
                IsConnected = false;

                // Запускаем автоматическое переподключение
                ScheduleReconnect();
            }
        }

        private void HandleBusyError()
        {
            try
            {
                Logger.Info("Attempting to recover from BUSY error...");

                // 1. Останавливаем текущий энкодер
                if (_encoder != null)
                {
                    Logger.Info("Stopping current encoder...");
                    _encoder.Dispose();
                    _encoder = null;
                }

                // 2. Даем время на освобождение ресурсов
                Thread.Sleep(2000);

                // 3. Создаем новый энкодер
                Logger.Info("Creating new encoder...");
                _encoder = new OpusEncoder(_config, _mixer);

                // 4. Пробуем подключиться снова
                Logger.Info("Retrying connection...");
                ConnectToIceCast();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to recover from BUSY error: {ex.Message}");
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            if (_isReconnecting || _disposed)
                return;

            _isReconnecting = true;

            Logger.Info("Scheduling reconnect in 10 seconds...");

            _reconnectThread = new Thread(() =>
            {
                try
                {
                    // Ждем перед переподключением
                    for (int i = 10; i > 0; i--)
                    {
                        if (_disposed) return;
                        Thread.Sleep(1000);
                    }

                    if (!_disposed && !IsConnected)
                    {
                        Logger.Info("Attempting to reconnect to IceCast...");
                        ConnectToIceCast();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Reconnect thread error: {ex.Message}");
                }
                finally
                {
                    _isReconnecting = false;
                }
            });

            _reconnectThread.IsBackground = true;
            _reconnectThread.Start();
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
            while (IsConnected && !_disposed)
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
            {
                Logger.Warning($"Cannot set metadata: IceCast is {(IsConnected ? "connected but encoder is null" : "not connected")}");
                return;
            }

            try
            {
                _encoder.SetMetadata(artist, title);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to set metadata: {ex.Message}");

                // Если ошибка связана с подключением, пробуем переподключиться
                if (ex.Message.Contains("BASS_ERROR_HANDLE") || ex.Message.Contains("BASS_ERROR_BUSY"))
                {
                    Logger.Info("Metadata error suggests connection issue, scheduling reconnect...");
                    IsConnected = false;
                    ScheduleReconnect();
                }
            }
        }

        public void CheckConnection()
        {
            if (!IsConnected && !_isReconnecting)
            {
                Logger.Info("IceCast connection lost, attempting to reconnect...");
                ScheduleReconnect();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            IsConnected = false;

            // Останавливаем поток переподключения
            if (_reconnectThread != null && _reconnectThread.IsAlive)
            {
                _reconnectThread.Join(1000);
            }

            // Освобождаем энкодер
            _encoder?.Dispose();
            _encoder = null;

            Logger.Info("IceCast client disposed");
        }
    }
}