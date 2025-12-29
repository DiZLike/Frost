namespace gainer.Tags
{
    public class AudioTags
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;

        public bool HasTags =>
            !string.IsNullOrWhiteSpace(Title) ||
            !string.IsNullOrWhiteSpace(Artist) ||
            !string.IsNullOrWhiteSpace(Genre);
    }
}