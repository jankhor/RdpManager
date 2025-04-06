using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace RdpManager.Views {
    public partial class AboutDialog : Window {
        public AboutDialog() {
            InitializeComponent();
            DataContext = this;
            LoadVersionInfo();
        }

        public string AppName { get; private set; } = "RDP Manager";
        public string VersionText { get; private set; } = "Version: Loading...";
        public string BuildInfoText { get; private set; } = string.Empty;
        public string CopyrightText { get; private set; } = string.Empty;

        private void LoadVersionInfo() {
            try {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                
                // Application name
                AppName = assembly.GetName().Name ?? "RDP Manager";
                
                // Version info (major.minor.build)
                var version = assembly.GetName().Version;
                VersionText = version != null 
                    ? $"Version: {version.Major}.{version.Minor}.{version.Build}" 
                    : "Version: Unknown";

                // Build info with changeset
                var productVersion = versionInfo.ProductVersion;
                var changeset = !string.IsNullOrEmpty(productVersion) && productVersion.Contains('+')
                    ? productVersion.Split('+')[1].Substring(0, 8)
                    : "unknown";
                
                BuildInfoText = $"Changeset: {changeset}\n" +
                               $"Built: {File.GetLastWriteTime(assembly.Location):yyyy-MM-dd HH:mm}";

                // Copyright info
                CopyrightText = versionInfo.LegalCopyright ?? 
                              $"© {DateTime.Now.Year} Your Company";
            } catch (Exception ex) {
                VersionText = "Version: Unavailable";
                BuildInfoText = $"Error loading version info: {ex.Message}";
                Debug.WriteLine($"Version info error: {ex}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}