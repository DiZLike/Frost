// File: CircularBuffer.cs
using System;

namespace FrostCast
{
    public class CircularBuffer
    {
        private byte[] _buffer;
        private int _writePosition;
        private int _size;
        private object _lock = new object();
        private int _lastOggPosition = -1;

        public CircularBuffer(int capacity)
        {
            _buffer = new byte[capacity];
            _writePosition = 0;
            _size = 0;
        }

        public void Write(byte[] data)
        {
            lock (_lock)
            {
                // Сохраняем позицию OGG заголовка если найден
                for (int i = 0; i < data.Length - 4; i++)
                {
                    if (data[i] == 'O' && data[i + 1] == 'g' &&
                        data[i + 2] == 'g' && data[i + 3] == 'S')
                    {
                        _lastOggPosition = (_writePosition + i) % _buffer.Length;
                    }
                }

                foreach (byte b in data)
                {
                    _buffer[_writePosition] = b;
                    _writePosition = (_writePosition + 1) % _buffer.Length;
                    _size = Math.Min(_size + 1, _buffer.Length);
                }
            }
        }

        public byte[] GetRecentData(int maxBytes)
        {
            lock (_lock)
            {
                if (_size == 0) return Array.Empty<byte>();

                int startPos;
                int bytesToRead = Math.Min(maxBytes, _size);

                // Если есть известная позиция OGG заголовка, начинаем с нее
                if (_lastOggPosition != -1)
                {
                    startPos = _lastOggPosition;

                    // Проверяем, что в буфере достаточно данных от этой позиции
                    int availableFromOgg = (_size - ((_lastOggPosition - _writePosition + _buffer.Length) % _buffer.Length) + _buffer.Length) % _buffer.Length;
                    if (availableFromOgg < bytesToRead)
                    {
                        // Если недостаточно данных, начинаем раньше
                        startPos = (_lastOggPosition - (bytesToRead - availableFromOgg) + _buffer.Length) % _buffer.Length;
                    }
                }
                else
                {
                    // Начинаем с текущей позиции минус maxBytes
                    startPos = (_writePosition - bytesToRead + _buffer.Length) % _buffer.Length;
                }

                byte[] result = new byte[bytesToRead];

                for (int i = 0; i < bytesToRead; i++)
                {
                    int pos = (startPos + i) % _buffer.Length;
                    result[i] = _buffer[pos];
                }

                return result;
            }
        }

        public byte[] GetAllData()
        {
            lock (_lock)
            {
                if (_size == 0) return Array.Empty<byte>();

                byte[] result = new byte[_size];

                if (_writePosition >= _size)
                {
                    // Данные непрерывные
                    Array.Copy(_buffer, _writePosition - _size, result, 0, _size);
                }
                else
                {
                    // Данные разорваны
                    int firstPart = _size - _writePosition;
                    Array.Copy(_buffer, _buffer.Length - firstPart, result, 0, firstPart);
                    Array.Copy(_buffer, 0, result, firstPart, _writePosition);
                }

                return result;
            }
        }
        public byte[] GetCompleteOggData()
        {
            lock (_lock)
            {
                if (_size == 0) return Array.Empty<byte>();

                // Находим последний полный OGG page
                int lastOggStart = -1;

                for (int i = 0; i < _size - 4; i++)
                {
                    int pos = (_writePosition - _size + i + _buffer.Length) % _buffer.Length;
                    int nextPos = (pos + 1) % _buffer.Length;
                    int nextPos2 = (pos + 2) % _buffer.Length;
                    int nextPos3 = (pos + 3) % _buffer.Length;

                    if (_buffer[pos] == 'O' && _buffer[nextPos] == 'g' &&
                        _buffer[nextPos2] == 'g' && _buffer[nextPos3] == 'S')
                    {
                        lastOggStart = i;
                    }
                }

                if (lastOggStart >= 0)
                {
                    // Возвращаем все данные от последнего OGG заголовка
                    byte[] result = new byte[_size - lastOggStart];

                    for (int i = 0; i < result.Length; i++)
                    {
                        int pos = (_writePosition - _size + lastOggStart + i + _buffer.Length) % _buffer.Length;
                        result[i] = _buffer[pos];
                    }

                    return result;
                }

                // Если не нашли OGG заголовок, возвращаем все
                return GetAllData();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _writePosition = 0;
                _size = 0;
                _lastOggPosition = -1;
            }
        }

        public int Size => _size;
        public int Capacity => _buffer.Length;
    }
}