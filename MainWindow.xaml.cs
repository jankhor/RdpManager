using System.ComponentModel;
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
                Icon = new System.Drawing.Icon("Resources/rdp_icon.ico"),
                Text = "RDP Manager",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            RefreshTrayMenu();
            
            _notifyIcon.DoubleClick += (s, e) => ShowConnectionsMenu();
        }

        private void RefreshTrayMenu()
        {
            if (_notifyIcon?.ContextMenuStrip == null) return;
            
            _notifyIcon.ContextMenuStrip.Items.Clear();
            
            // Add connections
            foreach (var connection in _viewModel.Connections)
            {
                var menuItem = new ToolStripMenuItem(connection.DisplayName);
                menuItem.Click += (s, e) => _viewModel.LaunchCommand.Execute(connection.FilePath);
                _notifyIcon.ContextMenuStrip.Items.Add(menuItem);
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

        private void ShowConnectionsMenu()
        {
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                // _notifyIcon.ContextMenuStrip.Show(Cursor.Position);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}
