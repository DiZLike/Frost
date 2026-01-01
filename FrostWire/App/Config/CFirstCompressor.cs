using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CFirstCompressor
    {
        public bool Enable { get; set; } = false;
        public bool Adaptive { get; set; } = true;
        public float Threshold { get; set; } = -20;
        public float Ratio { get; set; } = 3.0f;
        public float Attack { get; set; } = 20f;
        public float Release { get; set; } = 150f;
        public float Gain { get; set; } = 0f;
    }
}