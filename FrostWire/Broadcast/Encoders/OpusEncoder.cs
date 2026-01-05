using FrostWire.App;
using FrostWire.App.Config.Encoders;
using FrostWire.Audio;
using FrostWire.Core;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.EncOpus;

namespace FrostWire.Broadcast.Encoders
{
    public class OpusEncoder : IDisposable
    {
        private readonly AppConfig _config;       // Конфигурация приложения
        private readonly Mixer _mixer;            // Микшер аудио
        private int _encoderHandle;               // Хэндл энкодера BASS
        private string _encoderExe;               // Путь к исполняемому файлу opusenc
        private bool _disposed = false;           // Флаг освобождения ресурсов
        private readonly object _disposeLock = new object();  // Блокировка для потокобезопасности
        private readonly COpus _opusEncoder;

        public int Handle => _encoderHandle;      // Публичный доступ к хэндлу (только для чтения)

        // Конструктор
        public OpusEncoder(AppConfig config, Mixer mixer, BaseEncoder opus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));  // Проверка аргументов
            _mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
            _opusEncoder = opus as COpus;

            Initialize();  // Инициализация энкодера
        }

        // Основная инициализация энкодера
        private void Initialize()
        {
            Logger.Info("[OpusEncoder] Инициализация Opus энкодера...");

            _encoderExe = GetOpusEncPath();  // Получаем путь к opusenc
            if (!File.Exists(_encoderExe))
                throw new FileNotFoundException($"opusenc не найден: {_encoderExe}");  // Проверка существования

            string parameters = BuildParameters();  // Строим параметры командной строки

            _encoderHandle = BassEnc.BASS_Encode_Start(
                    _mixer.Handle,
                    $"{parameters}",
                    0,
                    null,
                    IntPtr.Zero
                );

            if (_encoderHandle == 0)  // Проверка успешности создания
                throw new Exception($"Не удалось создать Opus энкодер: {Bass.BASS_ErrorGetCode()}");

            Logger.Info($"[OpusEncoder] Opus энкодер: {_opusEncoder.Bitrate}кбит/с {_opusEncoder.Mode}");  // Лог успеха
        }

        // Проверка валидности энкодера (потокобезопасная)
        public bool IsValid()
        {
            lock (_disposeLock)  // Защита от состояния гонки
            {
                return !_disposed && _encoderHandle != 0;  // Не освобожден и хэндл валиден
            }
        }

        // Установка метаданных (ID3 тегов)
        public bool SetMetadata(string artist, string title)
        {
            lock (_disposeLock)  // Потокобезопасность
            {
                if (_disposed || _encoderHandle == 0)  // Проверка доступности
                {
                    Logger.Warning("[OpusEncoder] Попытка установить метаданные на недоступном энкодере");
                    return false;  // Энкодер недоступен
                }

                string metadata = $"--artist \"{artist}\" --title \"{title}\"";  // Формируем метаданные

                // Отправляем метаданные в энкодер
                return BassEnc_Opus.BASS_Encode_OPUS_NewStream(
                    _encoderHandle,
                    metadata,
                    BASSEncode.BASS_ENCODE_FP_16BIT
                );
                // Возвращаем результат (true/false), ошибки логируются в вызывающем коде
            }
        }

        // Освобождение ресурсов (IDisposable реализация)
        public void Dispose()
        {
            lock (_disposeLock)  // Потокобезопасное освобождение
            {
                if (_disposed) return;  // Уже освобождено
                _disposed = true;       // Помечаем как освобожденное

                if (_encoderHandle != 0)  // Если хэндл валиден
                {
                    BassEnc.BASS_Encode_Stop(_encoderHandle);  // Останавливаем кодирование
                    Bass.BASS_StreamFree(_encoderHandle);      // Освобождаем ресурсы BASS
                    _encoderHandle = 0;                        // Обнуляем хэндл
                    Logger.Debug("[OpusEncoder] Ресурсы энкодера освобождены");  // Лог
                }
            }
        }

        // Получение пути к opusenc в зависимости от ОС
        private string GetOpusEncPath()
        {
            if (_config.OS == "Windows")  // Windows
            {
                string archFolder = _config.Architecture == "X64" ? "win64" : "win32";  // Архитектура
                return Path.Combine(
                    _config.BaseDirectory,  // Базовый каталог приложения
                    "encs",                 // Папка с энкодерами
                    "opus",                 // Папка Opus
                    archFolder,             // win64 или win32
                    "opusenc.exe"           // Исполняемый файл
                );
            }
            else  // Linux (и другие Unix-подобные)
            {
                return "/usr/local/bin/opusenc";
            }
        }

        // Формирование параметров командной строки для opusenc
        private string BuildParameters()
        {
            // Собираем все параметры в одну строку
            return $"{_encoderExe} " +                      // Исполняемый файл
                   $"--bitrate {_opusEncoder.Bitrate} " +    // Битрейт
                   $"--{_opusEncoder.Mode} " +               // Режим (VBR/CBR)
                   $"--{_opusEncoder.ContentType} " +        // Тип контента (audio/voip)
                   $"--comp {_opusEncoder.Complexity} " +    // Сложность кодирования
                   $"--framesize {_opusEncoder.FrameSize} " + // Размер фрейма
                   "- -";                                   // stdin -> stdout
        }
    }
}