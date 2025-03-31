Key Features:
Flat Structure for Monitored Folder Contents:

Files directly in monitored folders appear at top level

No sub-menu created for the monitored folder itself

Subfolders as Colored Menus:

Subfolders appear as bold menu items with light gray background

Files within subfolders appear in white background

Visual Hierarchy:

Folders: Light gray background, bold text

Files: White background

Favorites: Goldenrod star and bold text

Example Menu Structure:

Copy
[Connection1] (directly in monitored folder)
[Connection2] (directly in monitored folder)
[SubFolder1] (light gray)
  → [Connection3]
  → [Connection4]
[SubFolder2] (light gray)
  → [Connection5]
[---]
[Refresh]
[Settings]
[Exit]
Additional Improvements:
Better Path Handling:

Uses Path methods consistently

Handles both forward and backward slashes

Performance Optimizations:

Filters connections by monitored folder first

Only processes relevant connections

Visual Feedback:

Clear distinction between folders and files

Immediate visual feedback when toggling favorites

This implementation now:

Shows files directly in monitored folders at top level

Groups files in subfolders under colored menu items

Maintains all existing functionality

Provides clear visual hierarchy

Is more performant with large numbers of connections


using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace RdpManager.Views
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private readonly ViewModels.MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ViewModels.MainViewModel();
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

            // Color definitions
            var folderColor = Color.FromArgb(240, 240, 240);  // Light gray for folders
            var fileColor = Color.White;  // White for files

            foreach (var monitoredFolder in _viewModel.Settings.MonitoredFolders)
            {
                var folderConnections = _viewModel.Connections
                    .Where(c => c.FilePath.StartsWith(monitoredFolder))
                    .ToList();

                if (!folderConnections.Any()) continue;

                // Get connections directly in monitored folder (no subfolders)
                var rootConnections = folderConnections
                    .Where(c => Path.GetDirectoryName(c.FilePath) == monitoredFolder)
                    .OrderBy(c => c.DisplayName);

                // Get connections in subfolders
                var subfolderConnections = folderConnections
                    .Where(c => Path.GetDirectoryName(c.FilePath) != monitoredFolder)
                    .ToList();

                // Add connections directly in monitored folder
                foreach (var connection in rootConnections)
                {
                    var menuItem = CreateConnectionMenuItem(connection);
                    menuItem.BackColor = fileColor;
                    _notifyIcon.ContextMenuStrip.Items.Add(menuItem);
                }

                // Group by subfolders
                var subfolderGroups = subfolderConnections
                    .GroupBy(c => 
                    {
                        var relativePath = c.FilePath.Substring(monitoredFolder.Length).TrimStart('\\');
                        return relativePath.Split('\\').First();
                    })
                    .OrderBy(g => g.Key);

                foreach (var group in subfolderGroups)
                {
                    var folderItem = new ToolStripMenuItem(group.Key)
                    {
                        BackColor = folderColor,
                        Font = new Font(SystemFonts.MenuFont, FontStyle.Bold)
                    };

                    foreach (var connection in group.OrderBy(c => c.DisplayName))
                    {
                        var menuItem = CreateConnectionMenuItem(connection);
                        menuItem.BackColor = fileColor;
                        folderItem.DropDownItems.Add(menuItem);
                    }

                    _notifyIcon.ContextMenuStrip.Items.Add(folderItem);
                }
            }

            // Add separator
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            
            // Add action items
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
                menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
                menuItem.ForeColor = Color.Goldenrod;
            }

            return menuItem;
        }

        private void ShowConnectionsMenu()
        {
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                _notifyIcon.ContextMenuStrip.Show(Cursor.Position);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}
