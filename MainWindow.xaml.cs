using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

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

            // Handle left mouse click
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

            // Get all connections with valid paths
            var validConnections = _viewModel.Connections
                .Where(c => !string.IsNullOrEmpty(c.FilePath))
                .ToList();

            // Add root-level connections first
            foreach (var connection in validConnections.Where(c => 
                string.IsNullOrEmpty(Path.GetDirectoryName(c.FilePath)) || 
                Path.GetDirectoryName(c.FilePath) == Path.GetPathRoot(c.FilePath)))
            {
                AddConnectionToMenu(connection);
            }

            // Group remaining connections by immediate parent folder
            var groupedConnections = validConnections
                .Where(c => 
                {
                    var dir = Path.GetDirectoryName(c.FilePath);
                    return !string.IsNullOrEmpty(dir) && dir != Path.GetPathRoot(c.FilePath);
                })
                .GroupBy(c => Path.GetFileName(Path.GetDirectoryName(c.FilePath)))
                .OrderBy(g => g.Key);

            // Add grouped connections
            foreach (var group in groupedConnections)
            {
                if (group.Count() == 1)
                {
                    AddConnectionToMenu(group.First());
                }
                else
                {
                    var folderMenu = new ToolStripMenuItem(group.Key);
                    foreach (var connection in group.OrderBy(c => c.DisplayName))
                    {
                        AddConnectionToMenu(connection, folderMenu);
                    }
                    _notifyIcon.ContextMenuStrip.Items.Add(folderMenu);
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

        private void AddConnectionToMenu(RdpConnection connection, ToolStripMenuItem? parentMenu = null)
        {
            var menuItem = new ToolStripMenuItem(
                connection.IsFavorite ? $"★ {connection.DisplayName}" : connection.DisplayName);
            
            // Right-click to toggle favorite
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

            // Bold font for favorites - using fully qualified FontStyle
            if (connection.IsFavorite)
            {
                menuItem.Font = new Font(menuItem.Font, System.Drawing.FontStyle.Bold);
            }

            if (parentMenu != null)
            {
                parentMenu.DropDownItems.Add(menuItem);
            }
            else
            {
                _notifyIcon?.ContextMenuStrip?.Items.Add(menuItem);
            }
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
