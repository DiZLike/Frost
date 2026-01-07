# FrostWire Radio - Инструкция по настройке и использованию

## Обзор приложения
FrostWire Radio - это программное обеспечение для автоматизированного вещания интернет-радиостанции. Приложение поддерживает:
- Автоматическое воспроизведение музыки из плейлистов;
- Потоковое вещание на IceCast серверы;
- Динамическое расписание смены плейлистов;
- Автоматическую обработку звука (компрессия, лимитирование, ReplayGain);
- Вставку джинглов и рекламных блоков;
- Отправку информации о треках на внешние серверы;
- Многопоточное вещание с разными битрейтами.

## Требования к системе
*Поддерживаемые ОС:*
- **Windows** (7/8/10/11, x64/x86)
- **Linux** (Ubuntu 20.04+, Debian 10+, Raspbian, x64/ARM)

*Минимальные требования:*
- 512 MB RAM
- 100 MB свободного места
- Аудиоустройство (реальное или виртуальное)
- .NET 6.0 Runtime

## Создание конфигурационного файла
**В папке config создайте или отредактируйте файл strimer.conf:**

```ini
[Audio]
device=0;														# ID аудиоустройства (-1 - устройство по умолчанию)
frequency=44100;												# Частота дискретизации (44100 или 48000)

[IceCast]
server=localhost;												# Адрес IceCast сервера
port=8000;														# Порт IceCast сервера
name=FrostWire Radio;											# Название потока
genre=Various;													# Жанр
username=source;												# Логин для IceCast
password=hackme;												# Пароль для IceCast

[Playlist]
list=C:\Users\Evgeny\Desktop\list.pls;							# Основной плейлист
dynamic_playlist=yes;											# Да - динамически обновлять плейлист
save_playlist_history=yes;										# Да - сохранять историю воспроизведения
artist_genre_history_enable=yes;								# Исключить повторение исполнителей/жанров
max_history=10;													# Записей в истории
max_attempts=5;													# Попыток найти нового исполнителя/жанра в случае повтора
schedule_enable=no;												# Да - использовать расписание
schedule=;														# Путь к файлу расписания JSON

[Encoder:live]													# Имя mount-point IceCast сервера: live
type=opus;														# Тип кодека (opus)
enable=yes;														# Включить энкодер
bitrate=128;													# Битрейт (кбит/с)
bitrate_mode=vbr;												# Режим битрейта: vbr/cbr
content_type=music;												# Тип контента: music/speech
complexity=10;													# Сложность кодирования
framesize=60;													# Размер фрейма (мс)

[Encoder:live-32]												# Дополнительный энкодер
bitrate=32;
bitrate_mode=vbr;
content_type=music;
complexity=10;
framesize=60;

[ReplayGain]
use_replay_gain=yes;											# Использовать ReplayGain
use_custom_gain=yes;											# Использовать кастомный ReplayGain из комментариев

[FirstCompressor]
enable=yes;														# Включить компрессор
adaptive=yes;													# Адаптивный режим (автоматическая настройка параметров под RMS стрека)
threshold=-20;													# Порог срабатывания (dB)
ratio=4.0;														# Коэффициент сжатия (X:1)
attack=3;														# Время атаки (мс)
release=100;													# Время восстановления (мс)
gain=5;															# Усиление после компрессии (dB)

[SecondCompressor]
enable=yes;
threshold=-20;
ratio=2.5;
attack=25;
release=250;
gain=8;

[Limiter]
enable=yes;														# Включить лимитер
threshold=-5.0;													# Предельный уровень (dB)
release=5;														# Время восстановления (мс)
gain=3;															# Усиление (dB)

# Отправка информации о треках на внешний сервер
[MyServer]
enable=no;														# Включить отправку
server=http://r.dlike.ru;										# URL сервера
key=key;														# Ключ доступа
add_song_info_page=add-history;									# Страница для добавления трека
add_song_info_number_var=number;								# Параметр: номер трека
add_song_info_title_var=title;									# Параметр: название трека
add_song_info_artist_var=artist;								# Параметр: исполнитель
add_song_info_link_var=link;									# Параметр: ссылка на файл
add_song_info_link_folder_on_server=http://r.dlike.ru/music;	# Базовый URL файлов
remove_file_prefix=radio/music;									# Префикс для удаления из пути

[Jingles]
enable=no;														# Включить джинглы
file=/home/admin/radio/config/jingles.json;						# Файл конфигурации джинглов
frequency=6;													# Частота вставки джинглов (каждый N-й трек)
random=yes;														# Случайный порядок воспроизведения

[Debug]
enable=yes;														# Включить отладочный режим
stack_view=no;													# Показывать стек вызовов
```

## Создание плейлиста
**Создайте текстовый файл с расширением .pls:**
```ini
track=C:\Music\Artist - Song 1.mp3?;
track=C:\Music\Artist - Song 2.flac?;
track=C:\Music\Artist - Song 3.opus?;
```

## Расширенные возможности
### Динамические плейлисты
- Включите dynamic_playlist=yes, чтобы приложение автоматически обновляло плейлист при изменении файла.

### История воспроизведения
- При save_playlist_history=yes приложение создает файлы .history для отслеживания воспроизведенных треков и предотвращения повторений.

### Расписание вещания (Schedule)
- Создайте JSON файл расписания:
```json
{
  "ScheduleItems": [
    {
      "Name": "Утренний эфир",
      "StartHour": 6,
      "StartMinute": 0,
      "EndHour": 12,
      "EndMinute": 0,
      "DaysOfWeek": [1, 2, 3, 4, 5, 6, 7],
      "PlaylistPath": "C:\\Playlists\\morning.pls"
    },
    {
      "Name": "Вечерний эфир",
      "StartHour": 18,
      "StartMinute": 0,
      "EndHour": 24,
      "EndMinute": 0,
      "DaysOfWeek": [1, 2, 3, 4, 5, 6, 7],
      "PlaylistPath": "C:\\Playlists\\evening.pls"
    }
  ]
}
```

- В конфиге укажите:
```ini
[Playlist]
schedule_enable=yes;
schedule=C:\путь\к\schedule.json;
```

### Джинглы (аудио-вставки)
- Создайте JSON файл джинглов:
```json
{
  "JingleItems": [
    {
      "Path": "C:\\Jingles\\station_id.mp3",
    },
    {
      "Path": "C:\\Jingles\\advert1.ogg",
    },
    {
      "Path": "C:\\Jingles\\advert2.flac",
    }
  ]
}
```

- Настройте в конфиге:
```ini
[Jingles]
enable=yes;
file=C:\путь\к\jingles.json;
frequency=3;    # Джингл каждые 3 трека
random=yes;     # Случайный порядок
```

### ReplayGain и RMS
**Приложение поддерживает:**
- Стандартные теги ReplayGain в аудиофайлах;
- Кастомное усиление через комментарии:
```ini
replay-gain=-3.5
rms=-18.2
```

### Многопоточное вещание
**Настройте несколько энкодеров с разными битрейтами:**
```ini
[Encoder:live_128]
type=opus;
enable=yes;
bitrate=128;
bitrate_mode=vbr;

[Encoder:live_64]
type=opus;
enable=yes;
bitrate=64;
bitrate_mode=vbr;

[Encoder:live_32]
type=opus;
enable=yes;
bitrate=32;
bitrate_mode=vbr;
```

### Обработка звука
**Адаптивный режим первого компрессора**
*Что такое адаптивный компрессор?*
- Адаптивный компрессор в FrostWire Radio — это интеллектуальная система обработки звука, которая автоматически настраивает параметры сжатия для каждого трека индивидуально, основываясь на его характеристиках громкости (rms) и ReplayGain метаданных.

**Когда использовать адаптивный режим?**
*Включите адаптивный режим (adaptive=yes), если:*
- Ваша музыкальная библиотека имеет сильный разброс по громкости;
- Вы хотите автоматически выравнивать громкость между треками без ручной настройки;
- Вам нужно сохранить динамику в тихих композициях и контролировать пики в громких;

*Отключите адаптивный режим (adaptive=no), если:*
- Ваша музыкальная библиотека уже нормализована (одинаковая громкость);
- Вы хотите полный контроль над параметрами компрессии;
- Вы обрабатываете определенный жанр музыки с известными характеристиками;

**Как работает адаптивный компрессор?**
*Алгоритм работы:*
- Анализ трека: При загрузке каждого трека система анализирует (из тегов):
- RMS (среднеквадратичное значение) - средний уровень громкости;
- ReplayGain значение - рекомендованное усиление/ослабление;

*Динамическая настройка параметров:*
- Порог (Threshold): автоматически рассчитывается на основе RMS трека;
- Коэффициент (Ratio): адаптируется к отклонению от целевого уровня;
- Времена (Attack/Release): настраиваются под динамику трека;
