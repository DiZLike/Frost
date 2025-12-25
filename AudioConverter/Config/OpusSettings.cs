using System.Text.Json.Serialization;

namespace OpusConverter.Config
{
    public class OpusSettings
    {
        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("vbr_mode")]
        public string Mode { get; set; }

        [JsonPropertyName("audio_type")]
        public string AudioType { get; set; }

        [JsonPropertyName("compression")]
        public int Complexity { get; set; }

        [JsonPropertyName("frame_size")]
        public int FrameSize { get; set; }

        public OpusSettings()
        {
            Bitrate = 128;
            Mode = "vbr";
            AudioType = "music";
            Complexity = 10;
            FrameSize = 20;
        }
    }
}
