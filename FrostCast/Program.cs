// File: Program.cs
using System;
using System.Threading;

namespace FrostCast
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FrostCast Icecast Server");
            Console.WriteLine("========================");

            int port = 8000;
            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int customPort))
                {
                    port = customPort;
                }
            }

            FrostCastServer server = new FrostCastServer(port);
            server.Start();

            Console.WriteLine("Press 'q' to quit");
            Console.WriteLine("Press 's' to show stream status");

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.S)
                {
                    // Показать статус потоков
                    ShowStreamStatus(server);
                }
            }

            server.Stop();
        }
        private static void ShowStreamStatus(FrostCastServer server)
        {
            Console.WriteLine("\n=== Stream Status ===");

            lock (server.Streams)
            {
                foreach (var stream in server.Streams)
                {
                    int listenerCount = server.GetListenerCount(stream.Key);
                    bool hasSource = stream.Value.Source != null && stream.Value.Source.Client.Connected;

                    Console.WriteLine($"Mount: {stream.Key}");
                    Console.WriteLine($"  Source: {(hasSource ? "Connected" : "Disconnected")}");
                    Console.WriteLine($"  Listeners: {listenerCount}");
                    Console.WriteLine($"  Name: {stream.Value.Name}");
                    Console.WriteLine($"  Bitrate: {stream.Value.Bitrate}kbps");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("====================\n");
        }
    }
}