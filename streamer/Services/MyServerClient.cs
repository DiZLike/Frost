using Strimer.App;
using Strimer.Core;
using System;
using System.Net.Http;
using System.Web; // Для HttpUtility

namespace Strimer.Services
{
    public class MyServerClient : IDisposable // 1. Добавляем IDisposable для корректного освобождения ресурсов
    {
        private readonly AppConfig _config;          // Конфигурация приложения
        private readonly HttpClient _httpClient;     // HTTP-клиент для отправки запросов
        private readonly bool _enabled;              // Флаг активности клиента

        public MyServerClient(AppConfig config)
        {
            _config = config; // Сохраняем конфигурацию
            // Проверяем, включен ли клиент и задан ли URL
            _enabled = config.MyServerEnabled && !string.IsNullOrWhiteSpace(config.MyServerUrl);

            if (_enabled)
            {
                _httpClient = new HttpClient();               // Создаем экземпляр HttpClient
                _httpClient.Timeout = TimeSpan.FromSeconds(5); // Устанавливаем таймаут 5 секунд
                Logger.Info("[MyServerClient] MyServer клиент инициализирован"); // Логируем инициализацию
            }
            else
            {
                _httpClient = null; // Если клиент отключен, оставляем null
            }
        }

        public void SendTrackInfo(int trackNumber, string artist, string title, string filename)
        {
            if (!_enabled)          // Проверяем, включен ли клиент
                return;             // Если нет - выходим из метода

            if (string.IsNullOrWhiteSpace(artist) ||   // Проверяем входные параметры
                string.IsNullOrWhiteSpace(title) ||    // на пустые значения
                string.IsNullOrWhiteSpace(filename))   // (новая проверка)
            {
                Logger.Warning("[MyServerClient] Попытка отправить пустые данные о треке");
                return;
            }

            try
            {
                // 2. Используем HttpUtility для безопасного построения query-строки
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["key"] = _config.MyServerKey; // Ключ доступа
                queryParams[_config.MyAddSongInfoNumberVar] = trackNumber.ToString(); // Номер трека
                queryParams[_config.MyAddSongInfoArtistVar] = artist; // Исполнитель
                queryParams[_config.MyAddSongInfoTitleVar] = title;   // Название трека

                // 3. Формируем полный путь к файлу на сервере
                string filePath = _config.MyAddSongInfoLinkFolderOnServer + filename;
                queryParams[_config.MyAddSongInfoLinkVar] = filePath; // Путь к файлу

                // 4. Собираем полный URL с помощью UriBuilder
                var uriBuilder = new UriBuilder(_config.MyServerUrl);
                uriBuilder.Path = _config.MyAddSongInfoPage; // Путь к странице
                uriBuilder.Query = queryParams.ToString();    // Query-параметры

                string url = uriBuilder.ToString(); // Полный URL для запроса

                // Отправляем GET запрос синхронно
                var response = _httpClient.GetAsync(url).Result; // Блокируем поток до получения ответа

                if (response.IsSuccessStatusCode) // Проверяем успешность запроса
                {
                    string responseText = response.Content.ReadAsStringAsync().Result; // Читаем ответ
                    // Логирование успешной отправки убрано (закомментированная строка удалена)
                }
                else
                {
                    // Логируем ошибку HTTP
                    Logger.Warning($"[MyServerClient] Не удалось отправить информацию о треке: {response.StatusCode}");
                }
            }
            catch (Exception ex) // Обрабатываем исключения
            {
                // Логируем ошибку
                Logger.Error($"[MyServerClient] Ошибка при отправке на MyServer: {ex.Message}");
            }
        }

        // 5. Реализуем IDisposable для освобождения ресурсов
        public void Dispose()
        {
            _httpClient?.Dispose(); // Освобождаем HttpClient, если он был создан
        }
    }
}