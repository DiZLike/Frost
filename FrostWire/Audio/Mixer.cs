using FrostWire.Core;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace FrostWire.Audio
{
    public class Mixer
    {
        private int _handle;
        private HashSet<int> _streams = new();  // HashSet для быстрого поиска и уникальности

        public int Handle => _handle;           // Публичный доступ к хэндлу микшера
        public bool IsValid => _handle != 0;    // Проверка валидности микшера

        public Mixer(int sampleRate)
        {
            _handle = BassMix.BASS_Mixer_StreamCreate(  // Создание микшерного потока
                sampleRate,
                2,                                      // Стерео (2 канала)
                BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT
            );
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_MIXER_BUFFER, 2000);
            int bufLenAsync = Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_MIXER_BUFFER);

            if (!IsValid)                               // Если микшер не создался
            {
                var error = Bass.BASS_ErrorGetCode();   // Получаем код ошибки
                throw new Exception($"Не удалось создать микшер. Ошибка: {error}");  // Критическая ошибка
            }

            Logger.Debug($"[Mixer] Микшер создан (handle: {_handle})");  // Логируем успешное создание
        }

        public bool AddStream(int stream)                // Добавление аудиопотока в микшер
        {
            if (!IsValid)                               // Проверка валидности микшера
            {
                Logger.Warning("[Mixer] Попытка добавить поток в невалидный микшер");
                return false;
            }

            if (_streams.Contains(stream))              // Если поток уже добавлен
            {
                Logger.Debug($"[Mixer] Поток {stream} уже в микшере");
                return true;                            // Возвращаем true, т.к. технически поток уже в микшере
            }

            bool success = BassMix.BASS_Mixer_StreamAddChannel(  // Добавление потока в микшер
                _handle,
                stream,
                BASSFlag.BASS_MIXER_CHAN_NORAMPIN
            );

            if (success)                                // Если успешно добавили
            {
                _streams.Add(stream);                   // Добавляем в коллекцию
                Logger.Debug($"[Mixer] Поток {stream} добавлен в микшер");
            }
            else                                        // Если не удалось добавить
            {
                var error = Bass.BASS_ErrorGetCode();   // Получаем ошибку
                Logger.Error($"[Mixer] Не удалось добавить поток {stream} в микшер: {error}");
            }

            return success;                             // Возвращаем результат операции
        }

        public bool RemoveStream(int stream)            // Удаление потока из микшера
        {
            if (!_streams.Contains(stream))             // Если потока нет в коллекции
                return false;                           // Ничего не делаем

            BassMix.BASS_Mixer_ChannelRemove(stream);   // Удаляем из микшера
            _streams.Remove(stream);                    // Удаляем из коллекции
            Logger.Debug($"[Mixer] Поток {stream} удален из микшера");
            return true;                                // Возвращаем успех
        }

        public void Clear()                             // Полная очистка микшера
        {
            foreach (var stream in _streams)            // Проходим по всем потокам
            {
                BassMix.BASS_Mixer_ChannelRemove(stream);  // Удаляем каждый
            }
            _streams.Clear();                           // Очищаем коллекцию
            Logger.Debug("[Mixer] Микшер очищен");               // Логируем
        }

        public void Dispose()                           // Освобождение ресурсов
        {
            if (IsValid)                                // Если микшер валиден
            {
                Clear();                                // Очищаем потоки
                Bass.BASS_StreamFree(_handle);          // Освобождаем хэндл
                _handle = 0;                            // Обнуляем хэндл
                Logger.Debug("[Mixer] Микшер освобожден");      // Логируем
            }
        }
    }
}