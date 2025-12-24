using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace strimer.Core
{
    // Представляет элемент джингла из JSON-файла
    public class JingleItem
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty; // Путь к файлу джингла
    }
}