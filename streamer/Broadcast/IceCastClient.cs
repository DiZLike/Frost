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

        private CancellationTokenSource _cts;
        private Task _monitoringTask;
        private readonly object _reconnectLock = new object();
        private readonly object _syncRoot = new object();
        private bool _reconnectScheduled = false;

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
            _cts = new CancellationTokenSource();
            Logger.Info("Инициализация IceCast клиента...");

            // Создаем энкодер
            _encoder = new OpusEncoder(_config, _mixer);

            StartConnection();
        }

        private void StartConnection()
        {
            try
            {
                // Освобождаем старый энкодер, если он существует
                _encoder?.Dispose();
                _encoder = null;

                // Создаем новый энкодер
                _encoder = new OpusEncoder(_config, _mixer);

                // Подключаемся к IceCast серверу
                if (ConnectToIceCast())
                {
                    // Запускаем мониторинг соединения
                    StartMonitoring();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при запуске подключения: {ex.Message}");
                ScheduleReconnect(5000);
            }
        }

        private bool ConnectToIceCast()
        {
            lock (_syncRoot)
            {
                try
                {
                    Logger.Info($"Подключение к IceCast: {_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}");

                    // 1. Останавливаем предыдущее подключение
                    if (_encoder != null)
                    {
                        Logger.Debug("Остановка предыдущего энкодера...");
                        _encoder.Dispose();
                        _encoder = null;
                    }

                    // 2. Создаем новый энкодер
                    _encoder = new OpusEncoder(_config, _mixer);

                    // 3. Даем время на инициализацию
                    Thread.Sleep(100);

                    // 4. Подключаемся
                    string url = $"http://{_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}";
                    string auth = $"{_config.IceCastUser}:{_config.IceCastPassword}";

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

                        // При ошибке ALREADY - ждем и пробуем еще раз
                        if (error == BASSError.BASS_ERROR_ALREADY)
                        {
                            Logger.Warning("Ошибка ALREADY, ожидание освобождения ресурсов...");
                            Thread.Sleep(2000);
                            return ConnectToIceCast(); // Рекурсивная попытка
                        }

                        return false;
                    }

                    IsConnected = true;
                    Listeners = 0;
                    Logger.Info($"✓ Подключено к IceCast: {url}");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Не удалось подключиться к IceCast: {ex.Message}");
                    IsConnected = false;
                    return false;
                }
            }
        }
        private void ResetEncoder()
        {
            try
            {
                Logger.Info("Сброс энкодера...");

                // Освобождаем старый энкодер
                _encoder?.Dispose();
                _encoder = null;

                // Создаем новый
                _encoder = new OpusEncoder(_config, _mixer);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при сбросе энкодера: {ex.Message}");
            }
        }

        private void ScheduleReconnect(int delayMs)
        {
            // Предотвращаем множественные переподключения
            lock (_syncRoot)
            {
                if (_disposed || _cts.IsCancellationRequested || _reconnectScheduled)
                    return;

                _reconnectScheduled = true;
            }

            Task.Delay(delayMs, _cts.Token).ContinueWith(t =>
            {
                lock (_syncRoot)
                {
                    _reconnectScheduled = false;

                    if (!_disposed && !_cts.IsCancellationRequested && !IsConnected)
                    {
                        Logger.Info("Попытка переподключения к IceCast...");
                        ConnectToIceCast();
                    }
                }
            }, _cts.Token);
        }

        private void StartMonitoring()
        {
            // Останавливаем предыдущий мониторинг
            _shouldMonitor = false;
            _monitoringThread?.Join(1000);

            _shouldMonitor = true;
            _monitoringThread = new Thread(MonitorConnection);
            _monitoringThread.IsBackground = true;
            _monitoringThread.Start();
        }

        private void MonitorConnection()
        {
            Logger.Info("Мониторинг точки монтирования icecast запущен");
            while (_shouldMonitor && !_disposed && !_cts.IsCancellationRequested)
            {
                try
                {
                    // Проверяем соединение каждые 30 секунд
                    Thread.Sleep(30000);

                    if (_disposed || !_shouldMonitor || _cts.IsCancellationRequested)
                        break;

                    // Проверяем маунт-поинт
                    bool mountPointExists = CheckMountPoint();

                    if (mountPointExists)
                    {
                        // Обновляем статистику слушателей
                        UpdateListenerStats();

                        if (!IsConnected)
                        {
                            Logger.Info("Маунт-поинт найден, но IsConnected=false. Восстанавливаем соединение...");
                            IsConnected = true;
                        }
                    }
                    else if (IsConnected)
                    {
                        // Маунт-поинт не найден, значит соединение потеряно
                        Logger.Warning("Маунт-поинт не найден. Соединение потеряно.");
                        IsConnected = false;

                        // Полностью пересоздаем энкодер при потере соединения
                        ResetEncoder();

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
            lock (_syncRoot)
            {
                if (!IsConnected || _encoder == null || _disposed)
                {
                    Logger.Warning($"Не удалось установить метаданные: энкодер недоступен");
                    return;
                }

                try
                {
                    bool success = _encoder.SetMetadata(artist, title);
                    if (success)
                        Logger.Info($"Метаданные обновлены: {artist} - {title}");
                    else
                        Logger.Warning($"Не удалось обновить метаданные");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Не удалось установить метаданные: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed) return;
                _disposed = true;

                _shouldMonitor = false;
                _cts?.Cancel();

                // Останавливаем поток мониторинга
                if (_monitoringThread != null && _monitoringThread.IsAlive)
                {
                    _monitoringThread.Join(3000);
                }

                // Освобождаем энкодер
                if (_encoder != null)
                {
                    _encoder.Dispose();
                    _encoder = null;
                }

                _cts?.Dispose();
                Logger.Info("IceCast клиент освобожден");
            }
        }
    }
}