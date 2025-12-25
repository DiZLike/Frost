using CueSplitter.Models;

namespace CueSplitter.Services
{
    public interface IMetadataService
    {
        void WriteFlacMetadata(string filePath, CueSheet cueSheet, CueTrack track);
    }

    public class MetadataService : IMetadataService
    {
        public void WriteFlacMetadata(string filePath, CueSheet cueSheet, CueTrack track)
        {
            // Теперь метаданные добавляются через ffmpeg в AudioProcessor
            // Этот метод может остаться для совместимости, но ничего не делает
            // Или можно удалить интерфейс и его использование
        }
    }
}