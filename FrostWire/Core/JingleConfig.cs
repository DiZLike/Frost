using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace strimer.Core
{
    // Конфигурация джинглов из JSON-файла
    public class JingleConfig
    {
        public List<JingleItem> JingleItems { get; set; } = new List<JingleItem>(); // Список джинглов
    }
}