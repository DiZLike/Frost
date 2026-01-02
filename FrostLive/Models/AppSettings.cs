using Newtonsoft.Json;
using System.Drawing;
using System.Windows.Forms;

namespace FrostLive.Models
{
    public class AppSettings
    {
        public int Volume { get; set; }
        public Point WindowLocation { get; set; }
        public Size WindowSize { get; set; }
        public FormWindowState WindowState { get; set; }

        public AppSettings()
        {
            Volume = 80;
            WindowLocation = Point.Empty;
            WindowSize = new Size(900, 600);
            WindowState = FormWindowState.Normal;
        }
    }
}