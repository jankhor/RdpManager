using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RdpManager.Views;

namespace RdpManager
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly RdpFileService _rdpService;
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        [ObservableProperty]
        private ObservableCollection<RdpConnection> _connections = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public IRelayCommand<string> LaunchCommand { get; }
        public IRelayCommand<string> ToggleFavoriteCommand { get; }
        public IRelayCommand OpenSettingsCommand { get; }

        public MainViewModel()
        {
            _rdpService = new RdpFileService();
            _settingsService = new SettingsService();
            _settings = _settingsService.LoadSettings();

            LaunchCommand = new RelayCommand<string>(LaunchRdp);
            ToggleFavoriteCommand = new RelayCommand<string>(ToggleFavorite);
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            LoadConnections();
        }

        private void LoadConnections()
        {
            var connections = _rdpService.FindRdpFiles(_settings.MonitoredFolders);

            var sortedConnections = _settings.ShowRecentFirst
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
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                _settings = _settingsService.LoadSettings();
                LoadConnections();
            }
        }
    }
}
