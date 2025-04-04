using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
// using System.Windows.Forms;
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

        private void RefreshTrayMenu( )
        {
             if (_notifyIcon?.ContextMenuStrip == null) return;
    
            _notifyIcon.ContextMenuStrip.Items.Clear();

            // Color definitions
            // var folderColor = Color.FromArgb(240, 240, 240);  // Light gray for folders
            var folderColor = Color.Blue;
            var folderBgColor = Color.Yellow;
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


                Font folderFont;

                if (System.Drawing.SystemFonts.MenuFont != null) {
                    folderFont = new Font( System.Drawing.SystemFonts.MenuFont.FontFamily, 9f, System.Drawing.FontStyle.Bold);
                } else {
                    // Fallback font if MenuFont is null
                    folderFont = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
                }

                foreach (var group in subfolderGroups)
                {
                    var folderItem = new ToolStripMenuItem(group.Key)
                    {
                        BackColor = folderBgColor,
                        ForeColor = folderColor,
                        Font = folderFont

                        //Font = new System.Drawing.Font (System.Drawing.Font, FontStyle.Bold)
                        // Font = new Font(ToolStripMenuItem.Font, System.Drawing.FontStyle.Bold);
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
