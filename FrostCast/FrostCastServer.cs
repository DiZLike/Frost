// File: FrostCastServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FrostCast
{
    public class FrostCastServer
    {
        private TcpListener _listener;
        private Dictionary<string, StreamInfo> _streams;
        private bool _isRunning;
        private int _port;
        private ClientHandler _clientHandler;

        public FrostCastServer(int port)
        {
            _port = port;
            _streams = new Dictionary<string, StreamInfo>();
            _isRunning = false;
            _clientHandler = new ClientHandler(this);
        }

        public Dictionary<string, StreamInfo> Streams => _streams;

        public void Start()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"FrostCast server started on port {_port}");
                Console.WriteLine($"Supported mount points: /live");
                Console.WriteLine($"Source password: hackme");
                Console.WriteLine($"Listening on: http://localhost:{_port}/live");

                Thread acceptThread = new Thread(AcceptConnections);
                acceptThread.IsBackground = true;
                acceptThread.Start();

                Console.WriteLine("Server is running. Press 'q' to quit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            lock (_streams)
            {
                foreach (var stream in _streams.Values)
                {
                    if (stream.BroadcastThread != null)
                    {
                        stream.BroadcastThread.IsRunning = false;
                        stream.BroadcastThread.StopEvent?.Set();
                        stream.BroadcastThread.Thread?.Join(1000);
                    }

                    if (stream.Source != null)
                    {
                        stream.Source.Disconnect();
                    }

                    stream.DisconnectAllListeners();

                    stream.NewAudioEvent?.Set();
                    stream.NewAudioEvent?.Close();
                }
                _streams.Clear();
            }

            Console.WriteLine("Server stopped");
        }

        public void AddStream(string mountPoint, StreamInfo streamInfo)
        {
            lock (_streams)
            {
                _streams[mountPoint] = streamInfo;
            }
        }

        public void RemoveStream(string mountPoint)
        {
            lock (_streams)
            {
                if (_streams.ContainsKey(mountPoint))
                {
                    _streams.Remove(mountPoint);
                }
            }
        }

        private void AcceptConnections()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    _clientHandler.HandleClientAsync(client);
                }
                catch (SocketException)
                {
                    // Server stopped
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting connection: {ex.Message}");
                }
            }
        }

        public StreamInfo GetStream(string mountPoint)
        {
            lock (_streams)
            {
                return _streams.TryGetValue(mountPoint, out StreamInfo streamInfo) ? streamInfo : null;
            }
        }

        public int GetListenerCount(string mountPoint)
        {
            var stream = GetStream(mountPoint);
            if (stream == null) return 0;

            lock (stream.LockObject)
            {
                return stream.Listeners.Count;
            }
        }
    }
}