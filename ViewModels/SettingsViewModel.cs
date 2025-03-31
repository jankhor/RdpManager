using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox; // Explicitly use WPF MessageBox

namespace RdpManager.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        
        [ObservableProperty]
        private AppSettings _settings;

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
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var path = dialog.SelectedPath;
                    if (Directory.Exists(path))
                    {
                        if (!Settings.MonitoredFolders.Contains(path))
                        {
                            Settings.MonitoredFolders.Add(path);
                            OnPropertyChanged(nameof(Settings));
                        }
                        else
                        {
                            MessageBox.Show("This folder is already being monitored",
                                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding folder: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void RemoveFolder(string? folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
    
            try
            {
                if (Settings.MonitoredFolders.Contains(folder))
                {
                    Settings.MonitoredFolders.Remove(folder);
                    OnPropertyChanged(nameof(Settings));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing folder: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
