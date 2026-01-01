using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CAudio
    {
        // Аудио настройки
        public int AudioDevice { get; set; } = 0;
        public int SampleRate { get; set; } = 44100;
    }
}
