using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using RdpManager.ViewModels;  // Add this using directive

namespace RdpManager
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            SetupSystemTray();

        }

        private void SetupSystemTray()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon("Resources/rdp_icon.ico"),
                Text = "RDP Manager",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            _notifyIcon.MouseClick += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowConnectionsMenu();
                }
            };

            RefreshTrayMenu();
        }

        private void RefreshTrayMenu()
        {
             if (_notifyIcon?.ContextMenuStrip == null) return;
    
            _notifyIcon.ContextMenuStrip.Items.Clear();

            // Use the Settings property instead of _settings
            if (_viewModel.Settings?.MonitoredFolders == null) return;

            // Build menu structure starting from monitored folders
            foreach (var monitoredFolder in _viewModel.Settings.MonitoredFolders)
            {
                var folderName = Path.GetFileName(monitoredFolder);
                if (string.IsNullOrEmpty(folderName))
                    folderName = monitoredFolder; // Fallback for root folders

                var folderConnections = _viewModel.Connections
                    .Where(c => c.FilePath.StartsWith(monitoredFolder))
                    .ToList();

                if (folderConnections.Count == 0)
                    continue;

                // Create menu item for this monitored folder
                var folderMenuItem = new ToolStripMenuItem(folderName);

                // Build structure for this monitored folder
                var relativeConnections = folderConnections
                    .Select(c => new {
                        Connection = c,
                        RelativePath = c.FilePath.Substring(monitoredFolder.Length).TrimStart(Path.DirectorySeparatorChar)
                    })
                    .ToList();

                foreach (var item in relativeConnections.Where(x => string.IsNullOrEmpty(Path.GetDirectoryName(x.RelativePath))))
                {
                    // Directly in monitored folder
                    folderMenuItem.DropDownItems.Add(CreateConnectionMenuItem(item.Connection));
                }

                // Group by subfolders
                var subfolderGroups = relativeConnections
                    .Where(x => !string.IsNullOrEmpty(Path.GetDirectoryName(x.RelativePath)))
                    .GroupBy(x => x.RelativePath.Split(Path.DirectorySeparatorChar)[0])
                    .OrderBy(g => g.Key);

                foreach (var group in subfolderGroups)
                {
                    var subfolderItem = new ToolStripMenuItem(group.Key);
                    folderMenuItem.DropDownItems.Add(subfolderItem);

                    foreach (var item in group)
                    {
                        var pathParts = item.RelativePath.Split(Path.DirectorySeparatorChar);
                        var currentMenu = subfolderItem;

                        // Handle nested subfolders
                        for (int i = 1; i < pathParts.Length - 1; i++)
                        {
                            var existing = currentMenu.DropDownItems
                                .OfType<ToolStripMenuItem>()
                                .FirstOrDefault(m => m.Text == pathParts[i]);

                            if (existing == null)
                            {
                                existing = new ToolStripMenuItem(pathParts[i]);
                                currentMenu.DropDownItems.Add(existing);
                            }
                            currentMenu = existing;
                        }

                        // Add the connection
                        currentMenu.DropDownItems.Add(CreateConnectionMenuItem(item.Connection));
                    }
                }

                _notifyIcon.ContextMenuStrip.Items.Add(folderMenuItem);
            }

            // Add action items
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            
            var refreshItem = new ToolStripMenuItem("Refresh");
            refreshItem.Click += (s, e) => {
                _viewModel.LoadConnections();
                RefreshTrayMenu();
            };
            _notifyIcon.ContextMenuStrip.Items.Add(refreshItem);
            
            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => _viewModel.OpenSettingsCommand.Execute(null);
            _notifyIcon.ContextMenuStrip.Items.Add(settingsItem);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Current.Shutdown();
            _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        }

        private ToolStripMenuItem CreateConnectionMenuItem(RdpConnection connection)
        {
            var menuItem = new ToolStripMenuItem(
                connection.IsFavorite ? $"★ {connection.DisplayName}" : connection.DisplayName);
            
            menuItem.MouseUp += (s, e) => 
            {
                if (e.Button == MouseButtons.Right)
                {
                    _viewModel.ToggleFavoriteCommand.Execute(connection.FilePath);
                    RefreshTrayMenu();
                }
                else if (e.Button == MouseButtons.Left)
                {
                    _viewModel.LaunchCommand.Execute(connection.FilePath);
                }
            };

            if (connection.IsFavorite)
            {
                menuItem.Font = new Font(menuItem.Font, System.Drawing.FontStyle.Bold);
            }

            return menuItem;
        }

        private void ShowConnectionsMenu()
        {
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                // Using Control.MousePosition instead of Cursor.Position
                _notifyIcon.ContextMenuStrip.Show(Control.MousePosition);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}
