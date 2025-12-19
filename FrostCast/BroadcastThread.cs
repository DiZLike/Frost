// File: BroadcastThread.cs
using System;
using System.Net.Sockets;
using System.Threading;

namespace FrostCast
{
    public class BroadcastThread
    {
        public Thread Thread { get; set; }
        public bool IsRunning { get; set; }
        public ManualResetEvent StopEvent { get; set; }
        private StreamInfo _streamInfo;

        public BroadcastThread(StreamInfo streamInfo)
        {
            _streamInfo = streamInfo;
            IsRunning = true;
            StopEvent = new ManualResetEvent(false);

            Thread = new Thread(BroadcastLoop);
            Thread.IsBackground = true;
            Thread.Start();
        }

        private void BroadcastLoop()
        {
            Console.WriteLine($"Broadcast thread started for {_streamInfo.MountPoint}");

            while (IsRunning)
            {
                try
                {
                    // Ждем новые данные или команду остановки
                    WaitHandle[] waitHandles = new WaitHandle[]
                    {
                        _streamInfo.NewAudioEvent,
                        StopEvent
                    };

                    int signaledHandle = WaitHandle.WaitAny(waitHandles, 1000);

                    if (signaledHandle == 1) // StopEvent
                    {
                        Console.WriteLine($"Broadcast thread stopping for {_streamInfo.MountPoint}");
                        break;
                    }

                    // Получаем ВСЕ данные из очереди и отправляем их
                    ProcessAudioQueue();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Broadcast error for {_streamInfo.MountPoint}: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Console.WriteLine($"Broadcast thread ended for {_streamInfo.MountPoint}");
        }

        private void ProcessAudioQueue()
        {
            lock (_streamInfo.LockObject)
            {
                // Обрабатываем все данные в очереди
                while (_streamInfo.AudioQueue.Count > 0)
                {
                    byte[] audioData = _streamInfo.AudioQueue.Dequeue();
                    if (audioData != null && audioData.Length > 0)
                    {
                        BroadcastAudioData(audioData);
                    }
                }

                // Сбрасываем событие после обработки всей очереди
                _streamInfo.NewAudioEvent.Reset();
            }
        }

        private void BroadcastAudioData(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return;

            List<TcpClient> disconnectedListeners = new List<TcpClient>();

            lock (_streamInfo.LockObject)
            {
                if (_streamInfo.Listeners.Count == 0)
                    return;

                foreach (var listener in _streamInfo.Listeners)
                {
                    try
                    {
                        // Быстрая проверка перед отправкой
                        if (!listener.Connected || listener.Client == null ||
                            !listener.Client.Connected || listener.Client.Poll(0, SelectMode.SelectError))
                        {
                            disconnectedListeners.Add(listener);
                            continue;
                        }

                        NetworkStream listenerStream = listener.GetStream();
                        if (listenerStream.CanWrite)
                        {
                            // Используем асинхронную отправку с таймаутом
                            listenerStream.WriteTimeout = 5000; // 5 секунд таймаут
                            listenerStream.Write(audioData, 0, audioData.Length);
                        }
                        else
                        {
                            disconnectedListeners.Add(listener);
                        }
                    }
                    catch (SocketException sockEx)
                    {
                        Console.WriteLine($"Socket error broadcasting: {sockEx.SocketErrorCode}");
                        disconnectedListeners.Add(listener);
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"IO error broadcasting: {ioEx.Message}");
                        disconnectedListeners.Add(listener);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"General error broadcasting: {ex.Message}");
                        disconnectedListeners.Add(listener);
                    }
                }

                // Удаляем отключившихся слушателей
                if (disconnectedListeners.Count > 0)
                {
                    foreach (var disconnected in disconnectedListeners)
                    {
                        _streamInfo.Listeners.Remove(disconnected);
                        try
                        {
                            disconnected.Close();
                            Console.WriteLine($"Disconnected listener removed (total: {_streamInfo.Listeners.Count})");
                        }
                        catch { }
                    }
                }
            }
        }
    }
}