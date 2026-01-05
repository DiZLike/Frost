using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CIcecast
    {
        // IceCast настройки
        public string Server { get; set; } = "localhost";
        public string Port { get; set; } = "8000";
        public string User { get; set; } = "source";
        public string Password { get; set; } = "hackme";
        public string Name { get; set; } = "Strimer Radio";
        public string Genre { get; set; } = "Various";
    }
}
