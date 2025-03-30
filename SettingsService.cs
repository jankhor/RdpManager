using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace RdpManager
{
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";
        private readonly string _appDataPath;

        public SettingsService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RdpManager");
            Directory.CreateDirectory(_appDataPath);
        }

        public AppSettings LoadSettings()
        {
            var path = Path.Combine(_appDataPath, SettingsFileName);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            var path = Path.Combine(_appDataPath, SettingsFileName);
            File.WriteAllText(path, JsonSerializer.Serialize(settings));
            UpdateStartupRegistration(settings.RunAtStartup);
        }

        private void UpdateStartupRegistration(bool runAtStartup)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
            if (runAtStartup)
            {
                key?.SetValue("RdpManager", Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
            }
            else
            {
                key?.DeleteValue("RdpManager", false);
            }
        }
    }
}
