// File: SourceClient.cs
using System.Net.Sockets;
using System.Threading;

namespace FrostCast
{
    public class SourceClient
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public Thread Thread { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Password { get; set; }
        public string MountPoint { get; set; }

        public void Disconnect()
        {
            try
            {
                Stream?.Close();
                Client?.Close();
            }
            catch { }
        }
    }
}