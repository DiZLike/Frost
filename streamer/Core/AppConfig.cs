using System.Runtime.InteropServices;

namespace Strimer.Core
{
    public class AppConfig
    {
        public string OS { get; private set; }
        public string Architecture { get; private set; }
        public bool IsConfigured { get; set; }

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
        public bool SavePlaylistHistory { get; set; } = true;

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

            Logger.Info($"Detected OS: {OS} ({Architecture})");
        }

        private void LoadConfig()
        {
            string configFile = Path.Combine(ConfigDirectory, "strimer.conf");

            if (!File.Exists(configFile))
            {
                Logger.Warning("Configuration file not found. Using defaults.");
                IsConfigured = false;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configFile);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Trim().Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim().TrimEnd(';');

                        SetValue(key, value);
                    }
                }

                IsConfigured = true;
                Logger.Info("Configuration loaded");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load config: {ex.Message}");
                IsConfigured = false;
            }
        }

        private void SetValue(string key, string value)
        {
            switch (key.ToLower())
            {
                case "app.configured":
                    IsConfigured = value.ToLower() == "yes";
                    break;

                case "icecast.server":
                    IceCastServer = value;
                    break;
                case "icecast.port":
                    IceCastPort = value;
                    break;
                case "icecast.link":
                    IceCastMount = value;
                    break;
                case "icecast.name":
                    IceCastName = value;
                    break;
                case "icecast.genre":
                    IceCastGenre = value;
                    break;
                case "icecast.username":
                    IceCastUser = value;
                    break;
                case "icecast.password":
                    IceCastPassword = value;
                    break;

                case "device.device":
                    AudioDevice = int.Parse(value);
                    break;
                case "device.frequency":
                    SampleRate = int.Parse(value);
                    break;

                case "radio.playlist":
                    PlaylistFile = value;
                    break;
                case "radio.save_playlist_history":
                    SavePlaylistHistory = value.ToLower() == "yes";
                    break;

                case "opus.bitrate":
                    OpusBitrate = int.Parse(value);
                    break;
                case "opus.bitrate_mode":
                    OpusMode = value;
                    break;
                case "opus.content_type":
                    OpusContentType = value;
                    break;
                case "opus.complexity":
                    OpusComplexity = int.Parse(value);
                    break;
                case "opus.framesize":
                    OpusFrameSize = value;
                    break;

                case "radio.use_replay_gain":
                    UseReplayGain = value.ToLower() == "yes";
                    break;
                case "radio.use_custom_gain":
                    UseCustomGain = value.ToLower() == "yes";
                    break;

                case "mysrv.enable":
                    MyServerEnabled = value.ToLower() == "yes";
                    break;
                case "mysrv.server":
                    MyServerUrl = value;
                    break;
                case "mysrv.key":
                    MyServerKey = value;
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
                    "# Strimer Radio Configuration",
                    "# Generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "",
                    "[App]",
                    $"app.configured=yes;",
                    "",
                    "[IceCast]",
                    $"icecast.server={IceCastServer};",
                    $"icecast.port={IceCastPort};",
                    $"icecast.link={IceCastMount};",
                    $"icecast.name={IceCastName};",
                    $"icecast.genre={IceCastGenre};",
                    $"icecast.username={IceCastUser};",
                    $"icecast.password={IceCastPassword};",
                    "",
                    "[Audio]",
                    $"device.device={AudioDevice};",
                    $"device.frequency={SampleRate};",
                    "",
                    "[Playlist]",
                    $"radio.playlist={PlaylistFile};",
                    $"radio.save_playlist_history={(SavePlaylistHistory ? "yes" : "no")};",
                    "",
                    "[Encoder]",
                    $"opus.bitrate={OpusBitrate};",
                    $"opus.bitrate_mode={OpusMode};",
                    $"opus.content_type={OpusContentType};",
                    $"opus.complexity={OpusComplexity};",
                    $"opus.framesize={OpusFrameSize};",
                    "",
                    "[Audio Processing]",
                    $"radio.use_replay_gain={(UseReplayGain ? "yes" : "no")};",
                    $"radio.use_custom_gain={(UseCustomGain ? "yes" : "no")};",
                    "",
                    "[External Services]",
                    $"mysrv.enable={(MyServerEnabled ? "yes" : "no")};",
                    $"mysrv.server={MyServerUrl};",
                    $"mysrv.key={MyServerKey};"
                };

                File.WriteAllLines(Path.Combine(ConfigDirectory, "strimer.conf"), lines);
                Logger.Info("Configuration saved");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save config: {ex.Message}");
            }
        }
    }
}