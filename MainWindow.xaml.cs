using System.ComponentModel;
using System.Drawing; // For Font, FontStyle
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Linq;
using System.Collections.Generic;

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

            RefreshTrayMenu();
            
            _notifyIcon.DoubleClick += (s, e) => ShowConnectionsMenu();
            
            // Handle left mouse click
            _notifyIcon.MouseClick += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowConnectionsMenu();
                }
            };
        }

        private void RefreshTrayMenu()
        {
            if (_notifyIcon?.ContextMenuStrip == null) return;
            
            _notifyIcon.ContextMenuStrip.Items.Clear();

            // Group connections by folder
            var groupedConnections = _viewModel.Connections
                .GroupBy(c => System.IO.Path.GetDirectoryName(c.FilePath) ?? "Other")
                .OrderBy(g => g.Key);

            foreach (var group in groupedConnections)
            {
                // If only one item in folder, add directly
                if (group.Count() == 1)
                {
                    var connection = group.First();
                    AddConnectionToMenu(connection);
                }
                else // Create submenu for folder
                {
                    var folderMenu = new ToolStripMenuItem(System.IO.Path.GetFileName(group.Key));
                    foreach (var connection in group.OrderBy(c => c.DisplayName))
                    {
                        AddConnectionToMenu(connection, folderMenu);
                    }
                    _notifyIcon.ContextMenuStrip.Items.Add(folderMenu);
                }
            }

            // Add separator
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            
            // Add refresh option
            var refreshItem = new ToolStripMenuItem("Refresh");
            refreshItem.Click += (s, e) => {
                _viewModel.LoadConnections();
                RefreshTrayMenu();
            };
            _notifyIcon.ContextMenuStrip.Items.Add(refreshItem);
            
            // Add settings option
            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => _viewModel.OpenSettingsCommand.Execute(null);
            _notifyIcon.ContextMenuStrip.Items.Add(settingsItem);
            
            // Add exit option
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Current.Shutdown();
            _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        }

        private void AddConnectionToMenu(RdpConnection connection, ToolStripMenuItem parentMenu = null)
        {
            var menuItem = new ToolStripMenuItem(
                connection.IsFavorite ? $"★ {connection.DisplayName}" : connection.DisplayName);
            
            // Add right-click to toggle favorite
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

            // Bold font for favorites
            if (connection.IsFavorite)
            {
                menuItem.Font = new Font(menuItem.Font, System.Drawing.FontStyle.Bold);
            }

            if (parentMenu != null)
                parentMenu.DropDownItems.Add(menuItem);
            else
                _notifyIcon?.ContextMenuStrip?.Items.Add(menuItem);
        }

        private void ShowConnectionsMenu()
        {
            _notifyIcon?.ContextMenuStrip?.Show(Control.MousePosition);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}
