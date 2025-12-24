using Strimer.Core;
using System.Runtime.InteropServices;

namespace Strimer.App
{
    public class AppConfig
    {
        public string OS { get; private set; }
        public string Architecture { get; private set; }
        public bool IsConfigured { get; set; } = false;

        // Пути
        public string BaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");

        // IceCast настройки
        public string IceCastServer { get; set; } = "localhost";
        public string IceCastPort { get; set; } = "8000";
        public string IceCastMount { get; set; } = "live";
        public string IceCastUser { get; set; } = "source";
        public string IceCastPassword { get; set; } = "hackme";
        public string IceCastName { get; set; } = "Strimer Radio";
        public string IceCastGenre { get; set; } = "Various";

        // Аудио настройки
        public int AudioDevice { get; set; } = -1;
        public int SampleRate { get; set; } = 44100;

        // Плейлист
        public string PlaylistFile { get; set; } = "playlist.txt";
        public bool DynamicPlaylist { get; set; } = false;
        public bool SavePlaylistHistory { get; set; } = true;

        // Расписание (НОВОЕ)
        public bool ScheduleEnable { get; set; } = false;
        public string ScheduleFile { get; set; } = "schedule.json";

        // Opus настройки
        public int OpusBitrate { get; set; } = 128;
        public string OpusMode { get; set; } = "vbr";
        public string OpusContentType { get; set; } = "music";
        public int OpusComplexity { get; set; } = 10;
        public string OpusFrameSize { get; set; } = "20";

        // Replay Gain
        public bool UseReplayGain { get; set; } = true;
        public bool UseCustomGain { get; set; } = false;

        // MyServer
        public bool MyServerEnabled { get; set; } = false;
        public string MyServerUrl { get; set; } = "";
        public string MyServerKey { get; set; } = "";
        public string MyAddSongInfoPage { get; set; } = "";
        public string MyAddSongInfoNumberVar { get; set; } = "";
        public string MyAddSongInfoTitleVar { get; set; } = "";
        public string MyAddSongInfoArtistVar { get; set; } = "";
        public string MyAddSongInfoLinkVar { get; set; } = "";
        public string MyAddSongInfoLinkFolderOnServer { get; set; } = "";

        // Джинглы
        public bool JinglesEnable { get; set; } = false;                    // Включены ли джинглы
        public string JingleConfigFile { get; set; } = "jingles.json";     // Путь к файлу конфигурации джинглов
        public int JingleFrequency { get; set; } = 3;                      // Частота джинглов (каждый N-й трек)
        public bool JinglesRandom { get; set; } = true;                    // Случайный порядок джинглов

        // Debug
        public bool DebugEnable { get; set; } = false;
        public bool DebugStackView { get; set; } = false;

        public AppConfig()
        {
            DetectOS();
            LoadConfig();
        }

        private void DetectOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                OS = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                OS = "Linux";
            else
                OS = "Unknown";

            Architecture = RuntimeInformation.ProcessArchitecture.ToString();

            Core.Logger.Info($"[AppConfig] Обнаружена ОС: {OS} ({Architecture})");
        }

        private void LoadConfig()
        {
            string configFile = Path.Combine(ConfigDirectory, "strimer.conf");

            if (!File.Exists(configFile))
            {
                Core.Logger.Warning("[AppConfig] Файл конфигурации не найден. Используются значения по умолчанию.");
                IsConfigured = false;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configFile);
                string currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Пропускаем пустые строки и комментарии
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Определяем секцию
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Trim('[', ']').ToLower();
                        continue;
                    }

                    var parts = trimmedLine.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim().TrimEnd(';');

                        // Обработка пустых значений
                        if (string.IsNullOrEmpty(value))
                        {
                            Core.Logger.Warning($"[AppConfig] Пустое значение для ключа '{key}' в секции '{currentSection}'. Используется значение по умолчанию.");
                            continue;
                        }
                        SetValue(currentSection, key, value);
                    }
                }
                Logger.AppConfig = this;

                Logger.Debug($"[AppConfig] Загружено: IceCast={IceCastServer}:{IceCastPort}, Плейлист={PlaylistFile}");
                Logger.Debug($"[AppConfig] Аудио: Устройство={AudioDevice}, Частота={SampleRate}Гц");
                Logger.Debug($"[AppConfig] Кодировщик: Opus {OpusBitrate}кбит/с {OpusMode}, RG={(UseReplayGain ? "Вкл" : "Выкл")}");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"[AppConfig] Не удалось загрузить конфигурацию: {ex.Message}");
                IsConfigured = false;
            }
        }

        private void SetValue(string section, string key, string value)
        {
            // Упрощенная обработка: учитываем только секцию, без префиксов в ключах
            switch (section)
            {
                case "app":
                    switch (key)
                    {
                        case "configured":
                            string cleanValue = value.ToLower().Trim();
                            IsConfigured = (cleanValue == "yes" || cleanValue == "true" || cleanValue == "1");
                            break;
                    }
                    break;

                case "icecast":
                    switch (key)
                    {
                        case "server":
                            IceCastServer = value;
                            break;
                        case "port":
                            IceCastPort = value;
                            break;
                        case "mount":
                            IceCastMount = value;
                            break;
                        case "name":
                            IceCastName = value;
                            break;
                        case "genre":
                            IceCastGenre = value;
                            break;
                        case "username":
                            IceCastUser = value;
                            break;
                        case "password":
                            IceCastPassword = value;
                            break;
                    }
                    break;

                case "audio":
                    switch (key)
                    {
                        case "device":
                            if (int.TryParse(value, out int deviceId))
                                AudioDevice = deviceId;
                            break;
                        case "frequency":
                            if (int.TryParse(value, out int freq))
                                SampleRate = freq;
                            break;
                    }
                    break;

                case "playlist":
                    switch (key)
                    {
                        case "list":
                            PlaylistFile = value;
                            break;
                        case "dynamic_playlist":
                            DynamicPlaylist = value.ToLower() == "yes";
                            break;
                        case "save_playlist_history":
                            SavePlaylistHistory = value.ToLower() == "yes";
                            break;
                        case "schedule_enable":
                            ScheduleEnable = value.ToLower() == "yes";
                            break;
                        case "schedule":
                            ScheduleFile = value;
                            break;
                    }
                    break;

                case "encoder":
                    switch (key)
                    {
                        case "bitrate":
                            if (int.TryParse(value, out int bitrate))
                                OpusBitrate = bitrate;
                            break;
                        case "bitrate_mode":
                            OpusMode = value;
                            break;
                        case "content_type":
                            OpusContentType = value;
                            break;
                        case "complexity":
                            if (int.TryParse(value, out int complexity))
                                OpusComplexity = complexity;
                            break;
                        case "framesize":
                            OpusFrameSize = value;
                            break;
                    }
                    break;

                case "audioprocessing":
                    switch (key)
                    {
                        case "use_replay_gain":
                            UseReplayGain = value.ToLower() == "yes";
                            break;
                        case "use_custom_gain":
                            UseCustomGain = value.ToLower() == "yes";
                            break;
                    }
                    break;

                case "mysrv":  // Более краткое название, как в старом формате
                    switch (key)
                    {
                        case "enable":
                            MyServerEnabled = value.ToLower() == "yes";
                            break;
                        case "server":
                            MyServerUrl = value;
                            break;
                        case "key":
                            MyServerKey = value;
                            break;
                        case "add_song_info_page":
                            MyAddSongInfoPage = value;
                            break;
                        case "add_song_info_number_var":
                            MyAddSongInfoNumberVar = value;
                            break;
                        case "add_song_info_title_var":
                            MyAddSongInfoTitleVar = value;
                            break;
                        case "add_song_info_artist_var":
                            MyAddSongInfoArtistVar = value;
                            break;
                        case "add_song_info_link_var":
                            MyAddSongInfoLinkVar = value;
                            break;
                        case "add_song_info_link_folder_on_server":
                            MyAddSongInfoLinkFolderOnServer = value;
                            break;
                    }
                    break;

                case "jingles":
                    switch (key)
                    {
                        case "enable":
                            JinglesEnable = value.ToLower() == "yes";
                            break;
                        case "file":
                            JingleConfigFile = value;
                            break;
                        case "frequency":
                            if (int.TryParse(value, out int frequency))
                                JingleFrequency = frequency;
                            break;
                        case "random":
                            JinglesRandom = value.ToLower() == "yes";
                            break;
                    }
                    break;
           

                case "debug":
                    switch (key)
                    {
                        case "enable":
                            DebugEnable = value.ToLower() == "yes";
                            break;
                        case "stack_view":
                            DebugStackView = value.ToLower() == "yes";
                            break;
                    }
                    break;

                default:
                    Core.Logger.Warning($"[AppConfig] Неизвестная секция конфигурации: {section}");
                    break;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDirectory);

                var lines = new List<string>
                {
                    "# Конфигурация Strimer Radio",
                    "# Сгенерировано " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "",
                    "[App]",
                    $"configured={(IsConfigured ? "yes" : "no")};",
                    "",
                    "[IceCast]",
                    $"server={IceCastServer};",
                    $"port={IceCastPort};",
                    $"mount={IceCastMount};",
                    $"name={IceCastName};",
                    $"genre={IceCastGenre};",
                    $"username={IceCastUser};",
                    $"password={IceCastPassword};",
                    "",
                    "[Audio]",
                    $"device={AudioDevice};",
                    $"frequency={SampleRate};",
                    "",
                    "[Playlist]",
                    $"list={PlaylistFile};",
                    $"save_playlist_history={(SavePlaylistHistory ? "yes" : "no")};",
                    $"dynamic_playlist={(DynamicPlaylist ? "yes" : "no")};",
                    $"schedule_enable={(ScheduleEnable ? "yes" : "no")};",
                    $"schedule={ScheduleFile};",
                    "",
                    "[Encoder]",
                    $"bitrate={OpusBitrate};",
                    $"bitrate_mode={OpusMode};",
                    $"content_type={OpusContentType};",
                    $"complexity={OpusComplexity};",
                    $"framesize={OpusFrameSize};",
                    "",
                    "[AudioProcessing]",
                    $"use_replay_gain={(UseReplayGain ? "yes" : "no")};",
                    $"use_custom_gain={(UseCustomGain ? "yes" : "no")};",
                    "",
                    "[MySrv]",  // Более краткое название
                    $"enable={(MyServerEnabled ? "yes" : "no")};",
                    $"server={MyServerUrl};",
                    $"key={MyServerKey};",
                    $"add_song_info_page={MyAddSongInfoPage};",
                    $"add_song_info_number_var={MyAddSongInfoNumberVar};",
                    $"add_song_info_title_var={MyAddSongInfoTitleVar};",
                    $"add_song_info_artist_var={MyAddSongInfoArtistVar};",
                    $"add_song_info_link_var={MyAddSongInfoLinkVar};",
                    $"add_song_info_link_folder_on_server={MyAddSongInfoLinkFolderOnServer};",
                    "",
                    $"[Jingles]",
                    $"enable={(JinglesEnable ? "yes" : "no")};",
                    $"file={JingleConfigFile}",
                    $"frequency={JingleFrequency}",
                    $"random={(JinglesRandom ? "yes" : "no")};",
                    "",
                    $"[Debug]",
                    $"enable={(DebugEnable ? "yes" : "no")};",
                    $"stack_view={(DebugStackView ? "yes" : "no")};"
                };

                File.WriteAllLines(Path.Combine(ConfigDirectory, "strimer.conf"), lines);
                Core.Logger.Info("[AppConfig] Конфигурация сохранена");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"[AppConfig] Не удалось сохранить конфигурацию: {ex.Message}");
            }
        }
    }
}