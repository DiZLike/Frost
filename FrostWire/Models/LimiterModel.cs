using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.Models
{
    public class LimiterModel
    {
        public float Threshold { get; set; } = -1f;
        public float Release { get; set; } = 150f;
        public float Gain { get; set; } = -1;
    }
}