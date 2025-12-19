// File: StreamInfo.cs
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace FrostCast
{
    public class StreamInfo
    {
        public string MountPoint { get; set; }
        public string ContentType { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string Bitrate { get; set; }
        public List<TcpClient> Listeners { get; set; }
        public CircularBuffer AudioBuffer { get; set; }
        public object LockObject { get; set; }
        public SourceClient Source { get; set; }
        public BroadcastThread BroadcastThread { get; set; }
        public Queue<byte[]> AudioQueue { get; set; }
        public ManualResetEvent NewAudioEvent { get; set; }

        public StreamInfo(string mountPoint)
        {
            MountPoint = mountPoint;
            ContentType = "audio/ogg";
            Name = "Untitled Stream";
            Genre = "Various";
            Description = "";
            IsPublic = true;
            Bitrate = "128";
            Listeners = new List<TcpClient>();

            // 2MB кольцевой буфер (примерно 13 секунд для 128kbps)
            AudioBuffer = new CircularBuffer(2 * 1024 * 1024);

            LockObject = new object();
            AudioQueue = new Queue<byte[]>();
            NewAudioEvent = new ManualResetEvent(false);
        }

        public void AddListener(TcpClient client)
        {
            lock (LockObject)
            {
                Listeners.Add(client);
            }
        }

        public void RemoveListener(TcpClient client)
        {
            lock (LockObject)
            {
                Listeners.Remove(client);
            }
        }

        public void DisconnectAllListeners()
        {
            lock (LockObject)
            {
                foreach (var listener in Listeners)
                {
                    try
                    {
                        listener?.Close();
                    }
                    catch { }
                }
                Listeners.Clear();
            }
        }

        public void StartBroadcastThread()
        {
            BroadcastThread = new BroadcastThread(this);
        }

        public void AddAudioData(byte[] data)
        {
            lock (LockObject)
            {
                // Сохраняем в кольцевой буфер
                AudioBuffer.Write(data);

                // Добавляем в очередь для трансляции
                byte[] dataCopy = new byte[data.Length];
                Array.Copy(data, 0, dataCopy, 0, data.Length);
                AudioQueue.Enqueue(dataCopy);
                NewAudioEvent.Set();
            }
        }

        public byte[] GetRecentAudioData()
        {
            lock (LockObject)
            {
                try
                {
                    // Вместо вычисления по битрейту, возвращаем полные OGG данные
                    return AudioBuffer.GetCompleteOggData();
                }
                catch
                {
                    // Если ошибка, возвращаем последние 128KB
                    return AudioBuffer.GetRecentData(Math.Min(128 * 1024, AudioBuffer.Size));
                }
            }
        }
    }
}