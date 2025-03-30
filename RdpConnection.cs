namespace RdpManager
{
    public class RdpConnection
    {
        public string FilePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? FolderPath { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.MinValue;
    }
}
