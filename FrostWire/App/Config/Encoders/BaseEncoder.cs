using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config.Encoders
{
    public abstract class BaseEncoder
    {
        public string Type { get; set; } = "opus"; // "opus", "mp3", "aac"
        public bool Enabled { get; set; }
        public string Mount { get; set; } = "live";
    }
}