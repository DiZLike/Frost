using strimer.Core;
using Strimer.App;
using Strimer.Audio;
using Strimer.Broadcast;
using Strimer.Core;
using System.Text.Json;

namespace Strimer.Services
{
    // Сервис управления джинглами
    public class JingleService
    {
        private readonly AppConfig _config;              // Конфигурация приложения
        private readonly List<string> _jingles;          // Список путей к файлам джинглов
        private readonly Random _random;                 // Генератор случайных чисел
        private int _currentIndex;                       // Текущий индекс для последовательного воспроизведения
        private int _trackCounter;                       // Счетчик треков для определения момента воспроизведения джингла

        public int TotalJingles => _jingles.Count;      // Общее количество загруженных джинглов
        public bool HasJingles => _jingles.Any();       // Есть ли доступные джинглы

        public JingleService(AppConfig config)
        {
            _config = config;                            // Сохраняем конфигурацию
            if (!_config.JinglesEnable)
            {
                Logger.Info($"[JingleService] Система джинглов отключена");
                return;
            }
            Logger.Info($"[JingleService] Система джинглов включена");
            _jingles = new List<string>();               // Инициализируем список джинглов
            _random = new Random();                      // Инициализируем генератор случайных чисел
            _currentIndex = 0;                           // Начинаем с первого джингла
            _trackCounter = 0;                           // Инициализируем счетчик треков

            LoadJingles();                               // Загружаем джинглы из файла
        }

        // Загрузка джинглов из JSON-файла
        private void LoadJingles()
        {
            try
            {
                // Проверяем наличие файла конфигурации
                if (string.IsNullOrEmpty(_config.JingleConfigFile) ||
                    !File.Exists(_config.JingleConfigFile))
                {
                    Logger.Warning($"[JingleService] Файл конфигурации джинглов не найден: {_config.JingleConfigFile}");
                    return;                               // Выходим если файл не найден
                }

                // Читаем JSON-файл
                var json = File.ReadAllText(_config.JingleConfigFile);

                // Десериализуем JSON в объект
                var jingleConfig = JsonSerializer.Deserialize<JingleConfig>(json);

                // Проверяем наличие списка джинглов
                if (jingleConfig?.JingleItems != null)
                {
                    foreach (var item in jingleConfig.JingleItems)
                    {
                        // Проверяем что путь не пустой и файл существует
                        if (!string.IsNullOrEmpty(item.Path) && File.Exists(item.Path))
                        {
                            _jingles.Add(item.Path);      // Добавляем валидный путь
                        }
                        else
                        {
                            Logger.Warning($"[JingleService] Файл джингла не найден: {item.Path}");
                        }
                    }
                }

                // Логируем результат загрузки
                Logger.Info($"[JingleService] Загружено {_jingles.Count} джинглов");
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки загрузки
                Logger.Error($"[JingleService] Ошибка загрузки джинглов: {ex.Message}");
            }
        }
        public bool ShouldPlayJingle()
        {
            // Проверяем все условия для воспроизведения джингла
            return _config.JinglesEnable &&               // Если джинглы включены
                   HasJingles &&                         // И есть доступные джинглы
                   _trackCounter % _config.JingleFrequency == 0 && // И достигнута нужная частота
                   _trackCounter > 0;                    // И это не первый трек
        }

        // Воспроизведение джингла
        public bool PlayJingle(Player player, IceCastClient iceCast)
        {
            try
            {
                // Проверяем доступность джинглов и возможность воспроизведения
                if (!HasJingles || player == null || iceCast == null)
                {
                    Logger.Warning("[JingleService] Невозможно воспроизвести джингл: недостаточно данных");
                    return false;
                }

                // Проверяем, нужно ли играть джингл (дублируем проверку для безопасности)
                if (!ShouldPlayJingle())
                {
                    Logger.Debug("[JingleService] Не нужно воспроизводить джингл по условиям частоты");
                    return false;
                }

                // Выбираем джингл в зависимости от настроек
                string? jingleFile = _config.JinglesRandom
                    ? GetRandomJingle()    // Случайный джингл
                    : GetNextJingle();     // Следующий джингл по порядку

                if (string.IsNullOrEmpty(jingleFile))     // Проверяем что джингл получен
                {
                    Logger.Warning("[JingleService] Не удалось получить джингл для воспроизведения");
                    return false;                         // Выходим если джингл не найден
                }

                // Логируем начало воспроизведения джингла
                Logger.Info($"[JingleService] Воспроизведение джингла: {Path.GetFileName(jingleFile)}");

                // Воспроизводим джингл через переданный player
                var jingleTrack = player.PlayTrack(jingleFile);

                if (jingleTrack != null)                  // Если джингл успешно начал играть
                {
                    // Отправляем метаданные джингла в поток через переданный iceCast
                    iceCast.SetMetadata("Джингл", Path.GetFileNameWithoutExtension(jingleFile));

                    // Сбрасываем счетчик после воспроизведения джингла (ПЕРЕМЕЩЕНО ИЗ RadioService)
                    _trackCounter = 0;

                    Logger.Debug($"[JingleService] Джингл '{Path.GetFileName(jingleFile)}' начал воспроизводиться, счетчик сброшен");
                    return true;                         // Возвращаем успешный результат
                }
                else
                {
                    Logger.Warning("[JingleService] Не удалось начать воспроизведение джингла");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки воспроизведения джингла
                Logger.Error($"[JingleService] Ошибка воспроизведения джингла: {ex.Message}");
                return false;
            }
        }

        // Увеличение счетчика треков (вызывается из RadioService после каждого трека)
        public void IncrementTrackCounter()
        {
            _trackCounter++;
            Logger.Debug($"[JingleService] Счетчик треков увеличен: {_trackCounter}");
        }

        // Сброс счетчика треков (например, при перезагрузке сервиса)
        public void ResetTrackCounter()
        {
            _trackCounter = 0;
            Logger.Info("[JingleService] Счетчик треков сброшен");
        }

        // Получение текущего значения счетчика (для отладки)
        public int GetTrackCounter()
        {
            return _trackCounter;
        }

        // Получить следующий джингл в порядке плейлиста
        public string? GetNextJingle()
        {
            if (!HasJingles)                              // Проверяем есть ли джинглы
                return null;

            // Получаем джингл по текущему индексу
            var jingle = _jingles[_currentIndex];

            // Увеличиваем индекс с зацикливанием
            _currentIndex = (_currentIndex + 1) % _jingles.Count;

            return jingle;                                // Возвращаем путь к джинглу
        }

        // Получить случайный джингл
        public string? GetRandomJingle()
        {
            if (!HasJingles)                              // Проверяем есть ли джинглы
                return null;

            // Возвращаем случайный джингл
            return _jingles[_random.Next(_jingles.Count)];
        }
    }
}