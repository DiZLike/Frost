using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CLimiter
    {
        public bool Enable { get; set; } = true;
        public float Threshold { get; set; } = -5f;
        public float Release { get; set; } = 150f;
        public float Gain { get; set; } = -1f;
    }
}