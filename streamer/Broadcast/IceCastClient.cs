using Strimer.App;
using Strimer.Audio;
using Strimer.Broadcast.Encoders;
using Strimer.Core;
using System.Diagnostics;
using System.Net;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;

namespace Strimer.Broadcast
{
    public class IceCastClient : IDisposable
    {
        private readonly AppConfig _config;                 // Конфигурация приложения
        private OpusEncoder _encoder;                       // Аудио энкодер
        private Mixer _mixer;                               // Микшер аудио
        private Thread _monitoringThread;                   // Поток мониторинга соединения
        private bool _disposed;                             // Флаг освобождения ресурсов
        private bool _shouldMonitor = true;                 // Флаг работы мониторинга
        private readonly object _lock = new object();       // Объект для синхронизации
        private Stopwatch _reconnectTime = new Stopwatch(); // Таймер для измерения времени переподключения
        private int _reconnectAttempts = 0;                 // Счётчик попыток переподключения

        public bool IsConnected { get; private set; }       // Состояние подключения
        public int Listeners { get; private set; }          // Текущее количество слушателей
        public int PeakListeners { get; private set; }      // Максимальное количество слушателей

        // Конструктор - принимает конфигурацию
        public IceCastClient(AppConfig config)
        {
            _config = config;
        }

        // Инициализация клиента
        public void Initialize(Mixer mixer)
        {
            _mixer = mixer;                               // Сохраняем микшер
            Logger.Info("Инициализация IceCast клиента...");

            ConnectToIceCast();                           // Подключаемся к серверу
            StartMonitoring();                            // Запускаем мониторинг
        }

        // Подключение к IceCast серверу
        private bool ConnectToIceCast()
        {
            lock (_lock)                                  // Защита от параллельных вызовов
            {
                try
                {
                    // Логируем параметры подключения
                    Logger.Info($"Подключение к IceCast: {_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}");

                    _encoder?.Dispose();                  // Освобождаем старый энкодер
                    _encoder = new OpusEncoder(_config, _mixer); // Создаем новый энкодер
                    Thread.Sleep(100);                    // Даем время на инициализацию

                    // Формируем URL и авторизацию
                    string url = $"http://{_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}";
                    string auth = $"{_config.IceCastUser}:{_config.IceCastPassword}";

                    // Инициализируем трансляцию через BASS
                    bool success = BassEnc.BASS_Encode_CastInit(
                        _encoder.Handle,                  // Хэндл энкодера
                        url,                              // URL сервера
                        auth,                             // Логин:пароль
                        "audio/ogg",                      // Тип контента
                        _config.IceCastName,              // Название потока
                        _config.IceCastGenre,             // Жанр
                        null, null, null,                 // Дополнительные параметры
                        _config.OpusBitrate,              // Битрейт
                        BASSEncodeCast.BASS_ENCODE_CAST_PUT // Метод отправки
                    );

                    if (!success)                         // Если подключение не удалось
                    {
                        var error = Bass.BASS_ErrorGetCode(); // Получаем код ошибки BASS
                        Logger.Error($"Не удалось подключиться: {error}");

                        // При ошибке "уже используется" ждем и пробуем снова
                        if (error == BASSError.BASS_ERROR_ALREADY)
                        {
                            Logger.Warning("Ресурсы заняты, ждем 2 секунды...");
                            Thread.Sleep(2000);
                            return ConnectToIceCast();    // Рекурсивный повтор
                        }

                        return false;                     // Возвращаем неудачу
                    }

                    IsConnected = true;                   // Устанавливаем флаг подключения
                    Logger.Info($"Подключено к IceCast: {url}");
                    return true;                          // Успешное подключение
                }
                catch (Exception ex)                      // Обработка исключений
                {
                    Logger.Error($"Ошибка подключения: {ex.Message}");
                    IsConnected = false;
                    return false;
                }
            }
        }

        // Запуск мониторинга соединения
        private void StartMonitoring()
        {
            _shouldMonitor = true;                        // Разрешаем мониторинг
            _monitoringThread = new Thread(MonitorConnection); // Создаем поток
            _monitoringThread.IsBackground = true;        // Фоновый поток
            _monitoringThread.Start();                    // Запускаем поток
        }

        // Основной цикл мониторинга
        private void MonitorConnection()
        {
            Logger.Info("Мониторинг запущен");

            while (_shouldMonitor && !_disposed)            // Основной цикл мониторинга
            {
                try
                {
                    Thread.Sleep(15000);                    // Пауза между проверками - 15 секунд
                    if (_disposed || !_shouldMonitor)       // Проверка флагов остановки
                        break;

                    bool isAlive = CheckMountPoint();       // Проверка доступности mount point

                    if (isAlive)                            // Если соединение активно
                    {
                        UpdateListenerStats();              // Обновление статистики
                        if (!IsConnected)                   // Если флаг соединения не установлен
                        {
                            IsConnected = true;             // Устанавливаем флаг соединения
                            _reconnectAttempts = 0;         // Сбрасываем счётчик попыток
                            _reconnectTime.Stop();          // Останавливаем таймер переподключения
                            Logger.Info("Соединение восстановлено (обнаружено при проверке)");
                        }
                    }
                    else                                    // Если соединение неактивно
                    {
                        if (IsConnected)                    // Если соединение только что потеряно
                        {
                            _reconnectTime.Restart();       // Запускаем/сбрасываем таймер переподключения
                            _reconnectAttempts = 1;         // Начинаем первую попытку
                            IsConnected = false;            // Сбрасываем флаг соединения
                            Logger.Warning("Соединение потеряно. Начинаем переподключение...");
                        }
                        else if (_reconnectTime.IsRunning)  // Если уже пытаемся переподключиться
                            _reconnectAttempts++;           // Увеличиваем счётчик попыток

                        // Пытаемся переподключиться
                        Logger.Info($"Попытка переподключения #{_reconnectAttempts}");
                        if (ConnectToIceCast())             // Вызываем метод подключения
                        {
                            IsConnected = true;             // Устанавливаем флаг при успехе
                            _reconnectAttempts = 0;         // Сбрасываем счётчик попыток
                            _reconnectTime.Stop();          // Останавливаем таймер
                            Logger.Info($"Соединение восстановлено за {_reconnectTime.Elapsed.TotalSeconds:F1} секунд");
                        }
                        else                                // Если переподключение не удалось
                        {
                            Logger.Warning($"Переподключение не удалось (попытка #{_reconnectAttempts})");
                            // Здесь можно добавить задержку между попытками
                            // Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception ex)                        // Обработка исключений в мониторинге
                {
                    Logger.Error($"Ошибка мониторинга: {ex.Message}");
                    Thread.Sleep(10000);                    // Увеличенная пауза при ошибке
                }
            }

            _reconnectTime.Stop();                          // Останавливаем таймер при выходе
            Logger.Info("Мониторинг остановлен");
        }

        // Метод для получения времени текущего переподключения (опционально)
        public TimeSpan GetCurrentReconnectTime()
        {
            return _reconnectTime.Elapsed;                  // Возвращаем прошедшее время
        }

        // Проверка доступности mount point
        private bool CheckMountPoint()
        {
            try
            {
                string statsUrl = $"http://{_config.IceCastServer}:{_config.IceCastPort}/status-json.xsl";
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 0, 5);                 // Таймаут 5 секунд
                    string json = client.GetStringAsync(statsUrl).Result;   // Загружаем статистику
                    return json.Contains($"/{_config.IceCastMount}");       // Ищем наш mount point
                }

                //using (var client = new WebClient())      // Создаем WebClient
                //{
                //    client.Timeout = 5000;                // Таймаут 5 секунд
                //    string json = client.DownloadString(statsUrl); // Загружаем статистику
                //    return json.Contains($"/{_config.IceCastMount}"); // Ищем наш mount point
                //}
            }
            catch
            {
                return false;                             // При ошибке считаем недоступным
            }
        }

        // Обновление статистики слушателей
        private void UpdateListenerStats()
        {
            try
            {
                string statsUrl = $"http://{_config.IceCastServer}:{_config.IceCastPort}/status-json.xsl";

                using (var client = new WebClient())
                {
                    string json = client.DownloadString(statsUrl);
                    string mountPoint = $"/{_config.IceCastMount}";

                    if (json.Contains(mountPoint))        // Если наш mount point существует
                    {
                        // Ищем значение listeners в JSON
                        int start = json.IndexOf("\"listeners\":") + 12;
                        int end = json.IndexOf(",", start);

                        if (start > 12 && end > start)    // Если нашли
                        {
                            string listenersStr = json.Substring(start, end - start);
                            if (int.TryParse(listenersStr, out int current))
                            {
                                Listeners = current;      // Обновляем текущее значение
                                if (current > PeakListeners)
                                    PeakListeners = current; // Обновляем пиковое значение
                            }
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки при обновлении статистики
            }
        }

        // Установка метаданных (название трека)
        public void SetMetadata(string artist, string title)
        {
            if (!IsConnected || _encoder == null || _disposed) // Проверка доступности
                return;

            try
            {
                _encoder.SetMetadata(artist, title);      // Отправка метаданных
                Logger.Info($"Метаданные: {artist} - {title}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка метаданных: {ex.Message}");
            }
        }
        // Освобождение ресурсов
        public void Dispose()
        {
            lock (_lock)                                  // Защита от параллельного вызова
            {
                if (_disposed) return;                   // Если уже освобождено
                _disposed = true;                         // Устанавливаем флаг

                _shouldMonitor = false;                   // Останавливаем мониторинг

                // Ждем завершения потока мониторинга
                if (_monitoringThread != null && _monitoringThread.IsAlive)
                {
                    _monitoringThread.Join(1000);         // Даем 1 секунду на завершение
                }

                _encoder?.Dispose();                      // Освобождаем энкодер
                Logger.Info("IceCast клиент остановлен"); // Логируем остановку
            }
        }
    }
}