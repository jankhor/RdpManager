using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

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
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!Settings.MonitoredFolders.Contains(dialog.SelectedPath))
                {
                    Settings.MonitoredFolders.Add(dialog.SelectedPath);
                    OnPropertyChanged(nameof(Settings));
                }
            }
        }

        private void RemoveFolder(string? folder)
        {
            if (folder != null && Settings.MonitoredFolders.Contains(folder))
            {
                Settings.MonitoredFolders.Remove(folder);
                OnPropertyChanged(nameof(Settings));
            }
        }

        private void Save()
        {
            _settingsService.SaveSettings(Settings);
            CloseWindow(true);
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
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
