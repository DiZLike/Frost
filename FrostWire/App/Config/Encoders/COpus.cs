using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config.Encoders
{
    public class COpus
    {
        // Opus настройки
        public int OpusBitrate { get; set; } = 128;
        public string OpusMode { get; set; } = "vbr";
        public string OpusContentType { get; set; } = "music";
        public int OpusComplexity { get; set; } = 10;
        public string OpusFrameSize { get; set; } = "20";
    }
}
