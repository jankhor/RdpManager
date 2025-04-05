using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace RdpManager.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MonitoredFolders))]
        private AppSettings _settings;

        public ICollection<string> MonitoredFolders => Settings.MonitoredFolders;

        public IRelayCommand AddFolderCommand { get; }
        public IRelayCommand<string> RemoveFolderCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();
            _settings = _settingsService.LoadSettings();

            AddFolderCommand = new RelayCommand(AddFolder);
            RemoveFolderCommand = new RelayCommand<string>(RemoveFolder);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void AddFolder()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var path = dialog.SelectedPath;
                if (!Settings.MonitoredFolders.Contains(path))
                {
                    Settings.MonitoredFolders.Add(path);
                    OnPropertyChanged(nameof(MonitoredFolders));
                }
                else
                {
                    MessageBox.Show("This folder is already being monitored",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void RemoveFolder(string? folder)
        {
            if (!string.IsNullOrEmpty(folder) && Settings.MonitoredFolders.Contains(folder))
            {
                Settings.MonitoredFolders.Remove(folder);
                OnPropertyChanged(nameof(MonitoredFolders));
            }
        }

        private void Save()
        {
            try
            {
                _settingsService.SaveSettings(Settings);
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is Views.SettingsWindow settingsWindow &&
                    settingsWindow.DataContext == this)
                {
                    settingsWindow.DialogResult = dialogResult;
                    settingsWindow.Close();
                    break;
                }
            }
        }
    }
}
