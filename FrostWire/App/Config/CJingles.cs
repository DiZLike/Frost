using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CJingles
    {
        // Джинглы
        public bool JinglesEnable { get; set; } = false;
        public string JingleConfigFile { get; set; } = "jingles.json";
        public int JingleFrequency { get; set; } = 6;
        public bool JinglesRandom { get; set; } = true;
    }
}
