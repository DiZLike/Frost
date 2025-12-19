using Strimer.App;
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
        private Thread _monitoringThread;
        private bool _disposed;
        private bool _shouldMonitor = true;

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
            Logger.Info("Инициализация IceCast клиента...");

            // Создаем энкодер
            _encoder = new OpusEncoder(_config, _mixer);

            // Подключаемся к IceCast серверу
            if (ConnectToIceCast())
            {
                // Запускаем мониторинг соединения
                StartMonitoring();
            }
        }

        private bool ConnectToIceCast()
        {
            try
            {
                Logger.Info($"Подключение к IceCast: {_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}");

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
                    Logger.Error($"Не удалось подключиться к IceCast: {error}");

                    // Пробуем переподключиться через 10 секунд
                    ScheduleReconnect(10000);
                    return false;
                }

                IsConnected = true;
                Logger.Info($"✓ Подключено к IceCast: {url}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось подключиться к IceCast: {ex.Message}");
                ScheduleReconnect(10000);
                return false;
            }
        }

        private void ScheduleReconnect(int delayMs)
        {
            Thread reconnectThread = new Thread(() =>
            {
                Thread.Sleep(delayMs);
                if (!_disposed && !IsConnected)
                {
                    Logger.Info("Попытка переподключения к IceCast...");
                    ConnectToIceCast();
                }
            });

            reconnectThread.IsBackground = true;
            reconnectThread.Start();
        }

        private void StartMonitoring()
        {
            _monitoringThread = new Thread(MonitorConnection);
            _monitoringThread.IsBackground = true;
            _monitoringThread.Start();
        }

        private void MonitorConnection()
        {
            Logger.Info("Мониторинг точки монтирования icecast запущен");
            while (_shouldMonitor && !_disposed)
            {
                try
                {
                    // Проверяем соединение каждые 30 секунд
                    Thread.Sleep(30000);

                    if (_disposed || !_shouldMonitor)
                        break;

                    // Проверяем маунт-поинт
                    bool mountPointExists = CheckMountPoint();

                    if (mountPointExists)
                    {
                        // Обновляем статистику слушателей
                        UpdateListenerStats();
                    }
                    else if (IsConnected)
                    {
                        // Маунт-поинт не найден, значит соединение потеряно
                        Logger.Warning("Маунт-поинт не найден. Соединение потеряно.");
                        IsConnected = false;

                        // Пробуем переподключиться
                        Logger.Info("Попытка восстановления соединения...");
                        if (ConnectToIceCast())
                        {
                            Logger.Info("Соединение восстановлено");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка мониторинга IceCast: {ex.Message}");

                    if (IsConnected)
                    {
                        IsConnected = false;
                        ScheduleReconnect(5000);
                    }
                }
            }
        }

        private bool CheckMountPoint()
        {
            try
            {
                string statsUrl = $"http://{_config.IceCastServer}:{_config.IceCastPort}/status-json.xsl";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string json = client.GetStringAsync(statsUrl).Result;

                    // Проверяем наличие нашего mount point в статистике
                    string mountPoint = $"/{_config.IceCastMount}";
                    return json.Contains(mountPoint);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка проверки mount point: {ex.Message}");
                return false;
            }
        }

        private void UpdateListenerStats()
        {
            try
            {
                string statsUrl = $"http://{_config.IceCastServer}:{_config.IceCastPort}/status-json.xsl";

                using (var client = new WebClient())
                {
                    string json = client.DownloadString(statsUrl);
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
            catch (Exception ex)
            {
                Logger.Error($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        public void SetMetadata(string artist, string title)
        {
            if (!IsConnected || _encoder == null)
            {
                Logger.Warning($"Не удалось установить метаданные: IceCast не подключен");
                return;
            }

            try
            {
                bool success = _encoder.SetMetadata(artist, title);
                if (success)
                    Logger.Info($"Метаданные обновлены: {artist} - {title}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось установить метаданные: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _shouldMonitor = false;
            IsConnected = false;

            // Останавливаем поток мониторинга
            if (_monitoringThread != null && _monitoringThread.IsAlive)
            {
                _monitoringThread.Join(1000);
            }

            // Освобождаем энкодер
            _encoder?.Dispose();
            _encoder = null;

            Logger.Info("IceCast клиент освобожден");
        }
    }
}