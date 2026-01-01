using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CReplayGain
    {
        // Replay Gain
        public bool UseReplayGain { get; set; } = true;
        public bool UseCustomGain { get; set; } = false;
    }
}
