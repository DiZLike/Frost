using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config.Encoders
{
    public class COpus : BaseEncoder
    {
        // Opus настройки
        public COpus()
        {
            Type = "opus";
        }
        public int Bitrate { get; set; } = 128;
        public string Mode { get; set; } = "vbr";
        public string ContentType { get; set; } = "music";
        public int Complexity { get; set; } = 10;
        public string FrameSize { get; set; } = "20";
    }
}