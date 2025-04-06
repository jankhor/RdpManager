using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RdpManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly RdpFileService _rdpService;
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private ObservableCollection<RdpConnection> _connections = new();

        [ObservableProperty]
        private AppSettings _settings; // This will generate a public Settings property

        public IRelayCommand<string> LaunchCommand { get; }
        public IRelayCommand<string> ToggleFavoriteCommand { get; }
        public IRelayCommand OpenSettingsCommand { get; }
        public IRelayCommand RefreshCommand { get; }

        public MainViewModel() {
            _rdpService = new RdpFileService();
            _settingsService = new SettingsService();
            Settings = _settingsService.LoadSettings(); // Use the generated property

            LaunchCommand = new RelayCommand<string>(LaunchRdp);
            ToggleFavoriteCommand = new RelayCommand<string>(ToggleFavorite);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            RefreshCommand = new RelayCommand(LoadConnections);

            LoadConnections();
        }

        public void LoadConnections() {
            var connections = _rdpService.FindRdpFiles(Settings.MonitoredFolders); // Use Settings property

            var sortedConnections = Settings.ShowRecentFirst // Use Settings property
                ? connections.OrderByDescending(c => c.LastUsed)
                            .ThenByDescending(c => c.IsFavorite)
                            .ThenBy(c => c.DisplayName)
                : connections.OrderByDescending(c => c.IsFavorite)
                            .ThenBy(c => c.DisplayName);

            Connections = new ObservableCollection<RdpConnection>(sortedConnections);
        }

        private void LaunchRdp(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _rdpService.LaunchRdpFile(filePath);
            }
        }

        private void ToggleFavorite(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _rdpService.ToggleFavorite(filePath);
                LoadConnections();
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                Settings = _settingsService.LoadSettings(); // Use Settings property
                LoadConnections();
            }
        }
    }
}
