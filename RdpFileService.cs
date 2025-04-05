using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RdpManager
{
    public class RdpFileService
    {
        private const string FavoritesFileName = "favorites.json";
        private const string RecentFileName = "recent.json";
        private readonly string _appDataPath;

        public RdpFileService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RdpManager");
            Directory.CreateDirectory(_appDataPath);
        }

        public List<RdpConnection> FindRdpFiles(System.Collections.ObjectModel.ObservableCollection<string> folders)
        {
            var connections = new List<RdpConnection>();
            
            foreach (var folder in folders.Where(Directory.Exists))
            {
                try
                {
                    var files = Directory.GetFiles(folder, "*.rdp", SearchOption.AllDirectories);
                    connections.AddRange(files.Select(file => new RdpConnection
                    {
                        FilePath = file,
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        FolderPath = Path.GetDirectoryName(file)
                    }));
                }
                catch { /* Skip unauthorized folders */ }
            }

            var favorites = LoadFavorites();
            var recent = LoadRecentConnections();

            foreach (var conn in connections)
            {
                conn.IsFavorite = favorites.Contains(conn.FilePath);
                conn.LastUsed = recent.TryGetValue(conn.FilePath, out var date) ? date : DateTime.MinValue;
            }

            return connections;
        }

        public void LaunchRdpFile(string filePath)
        {
            try
            {
                Process.Start("mstsc.exe", $"/f \"{filePath}\"");
                UpdateRecentConnection(filePath);
            }
            catch { /* Handle errors */ }
        }

        public void ToggleFavorite(string filePath)
        {
            var favorites = LoadFavorites();
            if (favorites.Contains(filePath))
                favorites.Remove(filePath);
            else
                favorites.Add(filePath);
            SaveFavorites(favorites);
        }

        private HashSet<string> LoadFavorites()
        {
            var path = Path.Combine(_appDataPath, FavoritesFileName);
            return File.Exists(path) 
                ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(path)) ?? new HashSet<string>()
                : new HashSet<string>();
        }

        private Dictionary<string, DateTime> LoadRecentConnections()
        {
            var path = Path.Combine(_appDataPath, RecentFileName);
            return File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, DateTime>>(File.ReadAllText(path)) ?? new Dictionary<string, DateTime>()
                : new Dictionary<string, DateTime>();
        }

        private void UpdateRecentConnection(string filePath)
        {
            var recent = LoadRecentConnections();
            recent[filePath] = DateTime.Now;
            SaveRecentConnections(recent);
        }

        private void SaveFavorites(HashSet<string> favorites)
        {
            var path = Path.Combine(_appDataPath, FavoritesFileName);
            File.WriteAllText(path, JsonSerializer.Serialize(favorites));
        }

        private void SaveRecentConnections(Dictionary<string, DateTime> recent)
        {
            var path = Path.Combine(_appDataPath, RecentFileName);
            File.WriteAllText(path, JsonSerializer.Serialize(recent));
        }
    }
}
