// File: ClientHandler.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrostCast
{
    public class ClientHandler
    {
        private FrostCastServer _server;

        public ClientHandler(FrostCastServer server)
        {
            _server = server;
        }

        public async void HandleClientAsync(TcpClient client)
        {
            await Task.Run(() => HandleClient(client));
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            string clientInfo = $"{client.Client.RemoteEndPoint}";

            try
            {
                Console.WriteLine($"New connection from: {clientInfo}");

                stream = client.GetStream();
                client.ReceiveTimeout = 5000;

                // Читаем запрос
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, 1024));

                if (bytesRead == 0)
                {
                    Console.WriteLine($"No data from {clientInfo}, closing connection");
                    return;
                }

                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Request from {clientInfo}:\n{request}");

                // Определяем тип клиента
                if (request.StartsWith("SOURCE", StringComparison.OrdinalIgnoreCase) ||
                    request.StartsWith("PUT", StringComparison.OrdinalIgnoreCase) ||
                    request.Contains("Authorization:") ||
                    request.Contains("ice-password:"))
                {
                    Console.WriteLine($"Detected as SOURCE client: {clientInfo}");
                    HandleSourceClient(client, stream, request);
                }
                else if (request.StartsWith("GET", StringComparison.OrdinalIgnoreCase) ||
                         request.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Detected as LISTENER client: {clientInfo}");
                    HandleListenerClient(client, stream, request);
                }
                else
                {
                    Console.WriteLine($"Unknown request type from {clientInfo}");
                    SendErrorResponse(stream, 400, "Bad Request");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientInfo}: {ex.Message}");
            }
            finally
            {
                try
                {
                    stream?.Close();
                    client.Close();
                    Console.WriteLine($"Connection closed: {clientInfo}");
                }
                catch { }
            }
        }

        private void HandleSourceClient(TcpClient client, NetworkStream stream, string request)
        {
            string mountPoint = "";

            try
            {
                Console.WriteLine($"=== Source Request ===");
                Console.WriteLine(request);
                Console.WriteLine($"======================");

                var requestInfo = ParseSourceRequest(request);
                mountPoint = requestInfo.MountPoint;

                if (string.IsNullOrEmpty(mountPoint))
                {
                    SendErrorResponse(stream, 400, "Bad Request - No mount point specified");
                    return;
                }

                // Проверяем пароль
                if (string.IsNullOrEmpty(requestInfo.Password) || requestInfo.Password != "hackme")
                {
                    SendErrorResponse(stream, 401, "Unauthorized - Invalid password");
                    return;
                }

                // Для PUT запросов с Expect: 100-continue отправляем промежуточный ответ
                if (requestInfo.HasExpectContinue)
                {
                    SendContinueResponse(stream);
                }

                // Отправляем окончательный OK source клиенту
                SendSourceResponse(stream, requestInfo.IsPutRequest, requestInfo.ContentType);

                // Создаем или обновляем поток
                StreamInfo streamInfo = GetOrCreateStream(mountPoint, requestInfo);

                // Устанавливаем источник
                SetupSourceClient(streamInfo, client, stream, requestInfo);

                Console.WriteLine($"Source connected: {mountPoint} - {requestInfo.Name} (Bitrate: {requestInfo.Bitrate}kbps, Public: {requestInfo.IsPublic})");

                // Читаем аудиоданные от source
                ReadAudioDataFromSource(streamInfo, client, stream, mountPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Source client error: {ex.Message}");
            }
        }

        private SourceRequestInfo ParseSourceRequest(string request)
        {
            var info = new SourceRequestInfo();
            string[] lines = request.Split('\n');

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimEnd('\r', '\n');

                if (trimmedLine.StartsWith("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    info.IsPutRequest = true;
                    string[] parts = trimmedLine.Split(' ');
                    if (parts.Length > 1)
                    {
                        info.MountPoint = parts[1];
                    }
                }
                else if (trimmedLine.StartsWith("SOURCE", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = trimmedLine.Split(' ');
                    if (parts.Length > 1)
                    {
                        info.MountPoint = parts[1];
                    }
                }
                else if (trimmedLine.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    ParseAuthorizationHeader(trimmedLine, info);
                }
                else if (trimmedLine.StartsWith("ice-password:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Password = trimmedLine.Substring("ice-password:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("ice-name:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Name = trimmedLine.Substring("ice-name:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("ice-genre:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Genre = trimmedLine.Substring("ice-genre:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("ice-url:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Url = trimmedLine.Substring("ice-url:".Length).Trim();
                    if (string.IsNullOrEmpty(info.Genre) && !string.IsNullOrEmpty(info.Url))
                    {
                        info.Genre = info.Url;
                    }
                }
                else if (trimmedLine.StartsWith("ice-description:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Description = trimmedLine.Substring("ice-description:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("ice-public:", StringComparison.OrdinalIgnoreCase))
                {
                    ParseIcePublic(trimmedLine, info);
                }
                else if (trimmedLine.StartsWith("ice-bitrate:", StringComparison.OrdinalIgnoreCase))
                {
                    info.Bitrate = trimmedLine.Substring("ice-bitrate:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                {
                    info.ContentType = trimmedLine.Substring("Content-Type:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("Expect:", StringComparison.OrdinalIgnoreCase))
                {
                    info.HasExpectContinue = trimmedLine.Contains("100-continue", StringComparison.OrdinalIgnoreCase);
                }
            }

            return info;
        }

        private void ParseAuthorizationHeader(string line, SourceRequestInfo info)
        {
            string authHeader = line.Substring("Authorization:".Length).Trim();
            if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                string base64Credentials = authHeader.Substring(6);
                try
                {
                    string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
                    string[] credParts = credentials.Split(':');
                    if (credParts.Length >= 2)
                    {
                        info.Password = credParts[1];
                    }
                }
                catch
                {
                    info.Password = "";
                }
            }
        }

        private void ParseIcePublic(string line, SourceRequestInfo info)
        {
            string publicValue = line.Substring("ice-public:".Length).Trim();
            info.IsPublic = publicValue == "1" || publicValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private void SendContinueResponse(NetworkStream stream)
        {
            string continueResponse = "HTTP/1.1 100 Continue\r\n\r\n";
            byte[] continueBytes = Encoding.ASCII.GetBytes(continueResponse);
            stream.Write(continueBytes, 0, continueBytes.Length);
            stream.Flush();
        }

        private void SendSourceResponse(NetworkStream stream, bool isPutRequest, string contentType)
        {
            string response;
            if (isPutRequest)
            {
                response = "HTTP/1.0 200 OK\r\n" +
                          "Server: FrostCast/1.0\r\n";
            }
            else
            {
                response = "ICY 200 OK\r\n";
            }

            response += "Cache-Control: no-cache\r\n" +
                       $"Content-Type: {contentType}\r\n" +
                       "\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();
        }

        private StreamInfo GetOrCreateStream(string mountPoint, SourceRequestInfo requestInfo)
        {
            StreamInfo streamInfo;
            bool isNewStream = false;

            lock (_server.Streams)
            {
                if (!_server.Streams.ContainsKey(mountPoint))
                {
                    streamInfo = new StreamInfo(mountPoint)
                    {
                        ContentType = requestInfo.ContentType,
                        Name = requestInfo.Name,
                        Genre = requestInfo.Genre,
                        Description = requestInfo.Description,
                        IsPublic = requestInfo.IsPublic,
                        Bitrate = requestInfo.Bitrate
                    };
                    _server.Streams[mountPoint] = streamInfo;
                    isNewStream = true;
                }
                else
                {
                    streamInfo = _server.Streams[mountPoint];
                    streamInfo.ContentType = requestInfo.ContentType;
                    streamInfo.Name = requestInfo.Name;
                    streamInfo.Genre = requestInfo.Genre;
                    streamInfo.IsPublic = requestInfo.IsPublic;
                    streamInfo.Bitrate = requestInfo.Bitrate;

                    // Очищаем буфер при новом источнике
                    streamInfo.AudioBuffer.Clear();
                }
            }

            // Запускаем поток трансляции если это новый поток
            if (isNewStream)
            {
                streamInfo.StartBroadcastThread();
            }

            return streamInfo;
        }

        private void SetupSourceClient(StreamInfo streamInfo, TcpClient client, NetworkStream stream, SourceRequestInfo requestInfo)
        {
            lock (streamInfo.LockObject)
            {
                // Удаляем старый источник если есть
                if (streamInfo.Source != null)
                {
                    try
                    {
                        streamInfo.Source.Disconnect();
                    }
                    catch { }
                }

                streamInfo.Source = new SourceClient
                {
                    Client = client,
                    Stream = stream,
                    IsAuthenticated = true,
                    Password = requestInfo.Password,
                    MountPoint = streamInfo.MountPoint
                };
            }
        }

        private void ReadAudioDataFromSource(StreamInfo streamInfo, TcpClient client, NetworkStream stream, string mountPoint)
        {
            DateTime lastDataTime = DateTime.Now;
            byte[] audioBuffer = new byte[8192];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = stream.Read(audioBuffer, 0, audioBuffer.Length);
                    if (bytesRead > 0)
                    {
                        lastDataTime = DateTime.Now;

                        // Добавляем аудиоданные в поток
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(audioBuffer, 0, receivedData, 0, bytesRead);
                        streamInfo.AddAudioData(receivedData);

                        Console.WriteLine($"Received {bytesRead} bytes from source at {DateTime.Now:HH:mm:ss}");
                    }
                    else
                    {
                        // Проверяем таймаут
                        if ((DateTime.Now - lastDataTime).TotalSeconds > 10)
                        {
                            Console.WriteLine($"Source timeout for {mountPoint}");
                            break;
                        }
                        Thread.Sleep(100);
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"Source read error (IO): {ioEx.Message}");
            }
            catch (SocketException sockEx)
            {
                Console.WriteLine($"Source read error (Socket): {sockEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Source read error: {ex.Message}");
            }

            Console.WriteLine($"Source disconnected: {mountPoint}");

            // Удаляем источник при отключении
            lock (streamInfo.LockObject)
            {
                if (streamInfo.Source != null && streamInfo.Source.Client == client)
                {
                    streamInfo.Source = null;
                }
            }
        }

        private void HandleListenerClient(TcpClient client, NetworkStream stream, string request)
        {
            string requestedPath = "";
            bool icyMetaData = false;

            try
            {
                Console.WriteLine($"=== New Listener Request ===");
                Console.WriteLine(request);
                Console.WriteLine($"===========================");

                // Парсим запрос
                string[] lines = request.Split('\n');
                foreach (string line in lines)
                {
                    string trimmedLine = line.TrimEnd('\r', '\n');

                    if (trimmedLine.StartsWith("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = trimmedLine.Split(' ');
                        if (parts.Length > 1)
                        {
                            requestedPath = parts[1];
                        }
                    }
                    else if (trimmedLine.StartsWith("Icy-MetaData:", StringComparison.OrdinalIgnoreCase))
                    {
                        icyMetaData = trimmedLine.Substring("Icy-MetaData:".Length).Trim() == "1";
                    }
                }

                // Проверяем путь
                if (string.IsNullOrEmpty(requestedPath))
                {
                    SendErrorResponse(stream, 400, "Bad Request");
                    return;
                }

                // Ищем поток
                StreamInfo streamInfo = _server.GetStream(requestedPath);
                if (streamInfo == null)
                {
                    SendErrorResponse(stream, 404, "Not Found - Stream not available");
                    return;
                }

                // Проверяем источник
                if (streamInfo.Source == null || !streamInfo.Source.Client.Connected)
                {
                    SendErrorResponse(stream, 503, "Service Unavailable - No source connected");
                    return;
                }

                // Отправляем ответ
                SendListenerResponse(stream, streamInfo, icyMetaData);

                // Добавляем слушателя
                streamInfo.AddListener(client);
                Console.WriteLine($"Listener connected to: {streamInfo.MountPoint} (Total: {streamInfo.Listeners.Count})");
                // ОТПРАВЛЯЕМ начальные данные из буфера
                SendRecentAudioData(stream, streamInfo);

                Console.WriteLine("Initial audio data sent, waiting for broadcast...");
                // НЕ отправляем сразу последние данные - они будут приходить из BroadcastThread
                // Просто удерживаем соединение открытым
                KeepListenerConnectionSimple(client, streamInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Listener client error: {ex.Message}");
            }
            finally
            {
                // Удаляем слушателя
                if (!string.IsNullOrEmpty(requestedPath))
                {
                    StreamInfo streamInfo = _server.GetStream(requestedPath);
                    if (streamInfo != null)
                    {
                        streamInfo.RemoveListener(client);
                        Console.WriteLine($"Listener removed from: {requestedPath}. Total listeners: {streamInfo.Listeners.Count}");
                    }
                }

                try
                {
                    stream?.Close();
                    client.Close();
                    Console.WriteLine($"Listener connection closed");
                }
                catch { }
            }
        }

        private void SendListenerResponse(NetworkStream stream, StreamInfo streamInfo, bool icyMetaData)
        {
            string response = "HTTP/1.0 200 OK\r\n" +
                             "Server: FrostCast/1.0\r\n" +
                             "Cache-Control: no-cache\r\n" +
                             "Pragma: no-cache\r\n" +
                             $"Content-Type: {streamInfo.ContentType}\r\n" +
                             $"icy-name: {streamInfo.Name}\r\n" +
                             $"icy-genre: {streamInfo.Genre}\r\n" +
                             $"icy-description: {streamInfo.Description}\r\n" +
                             $"icy-pub: {(streamInfo.IsPublic ? "1" : "0")}\r\n" +
                             $"icy-br: {streamInfo.Bitrate}\r\n";

            // Добавляем icy-metaint если запрошены метаданные
            if (icyMetaData)
            {
                response += "icy-metaint: 16384\r\n";
            }
            else
            {
                response += "icy-metaint: 0\r\n";
            }

            response += "icy-url: http://localhost:8000" + streamInfo.MountPoint + "\r\n" +
                       "Access-Control-Allow-Origin: *\r\n" +
                       "Access-Control-Allow-Headers: *\r\n" +
                       "Access-Control-Allow-Methods: GET, OPTIONS\r\n" +
                       "\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();

            Console.WriteLine($"Sent response headers for: {streamInfo.MountPoint}");
        }

        private void SendRecentAudioData(NetworkStream stream, StreamInfo streamInfo)
        {
            try
            {
                // Получаем ВСЕ данные из буфера
                byte[] recentData = streamInfo.AudioBuffer.GetAllData();

                if (recentData.Length > 0)
                {
                    Console.WriteLine($"Sending {recentData.Length} bytes of buffer data to new listener");

                    // Ищем последний OGG заголовок для отправки корректного потока
                    int lastOggStart = FindLastOggPageStart(recentData);

                    if (lastOggStart >= 0 && lastOggStart < recentData.Length)
                    {
                        // Отправляем начиная с последнего полного OGG page
                        int bytesToSend = recentData.Length - lastOggStart;
                        byte[] dataToSend = new byte[bytesToSend];
                        Array.Copy(recentData, lastOggStart, dataToSend, 0, bytesToSend);

                        // Отправляем небольшими частями с задержкой
                        int chunkSize = 4096;
                        for (int i = 0; i < dataToSend.Length; i += chunkSize)
                        {
                            int size = Math.Min(chunkSize, dataToSend.Length - i);
                            stream.Write(dataToSend, i, size);
                            Thread.Sleep(20); // Увеличиваем задержку для стабильности
                        }
                        stream.Flush();

                        Console.WriteLine($"Sent {dataToSend.Length} bytes (from OGG page at position {lastOggStart})");
                    }
                    else
                    {
                        // Если не нашли OGG заголовок, отправляем все
                        Console.WriteLine("No OGG header found, sending all buffer data");
                        stream.Write(recentData, 0, recentData.Length);
                        stream.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending recent audio data: {ex.Message}");
            }
        }

        private int FindLastOggPageStart(byte[] data)
        {
            // Ищем "OggS" в обратном порядке
            for (int i = data.Length - 4; i >= 0; i--)
            {
                if (data[i] == 'O' && data[i + 1] == 'g' &&
                    data[i + 2] == 'g' && data[i + 3] == 'S')
                {
                    return i;
                }
            }
            return -1;
        }
        private void KeepListenerConnectionSimple(TcpClient client, StreamInfo streamInfo)
        {
            try
            {
                int checkCount = 0;

                while (client.Connected)
                {
                    Thread.Sleep(1000);
                    checkCount++;

                    // Каждые 5 секунд проверяем источник
                    if (checkCount % 5 == 0)
                    {
                        if (streamInfo.Source == null || !streamInfo.Source.Client.Connected)
                        {
                            Console.WriteLine("Source disconnected, closing listener");
                            break;
                        }

                        // Простая проверка состояния сокета
                        if (client.Client.Poll(0, SelectMode.SelectError))
                        {
                            Console.WriteLine("Socket error detected");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Keep-alive ended: {ex.Message}");
            }
        }

        private void SendErrorResponse(NetworkStream stream, int code, string message)
        {
            string response = $"HTTP/1.0 {code} {message}\r\n" +
                             "Server: FrostCast/1.0\r\n" +
                             "Content-Type: text/html\r\n" +
                             "\r\n" +
                             $"<html><body><h1>{code} {message}</h1></body></html>";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
        }

        private class SourceRequestInfo
        {
            public string MountPoint { get; set; } = "";
            public string ContentType { get; set; } = "audio/ogg";
            public string Password { get; set; } = "";
            public string Name { get; set; } = "Untitled Stream";
            public string Genre { get; set; } = "Various";
            public string Description { get; set; } = "";
            public string Url { get; set; } = "";
            public string Bitrate { get; set; } = "128";
            public bool IsPublic { get; set; } = true;
            public bool IsPutRequest { get; set; } = false;
            public bool HasExpectContinue { get; set; } = false;
        }
    }
}