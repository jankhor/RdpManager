using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
// using System.Windows.Forms;
using Application = System.Windows.Application;
using RdpManager.ViewModels;  // Add this using directive
using System.Reflection; // Required for Assembly
using Serilog;
using Serilog.Sinks.File;  // Add this for RollingInterval
using Serilog.Context;


namespace RdpManager {
    public partial class MainWindow : Window {
        private NotifyIcon? _notifyIcon;
        private readonly MainViewModel _viewModel;
        private Font? folderFont=null;
        private Image? folderImage=null;
        private Image? connectionImage=null;

        private readonly ILogger _logger;

        public MainWindow() {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RdpManager");
            var     logFile = Path.Combine(appDataPath, "rdpManager.log");

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .Enrich.FromLogContext()  // REQUIRED for LogContext properties
                .WriteTo.Console()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
                         outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}.{Method}: {Message:lj}{NewLine}{Exception}"
                ).CreateLogger();

            _logger = Log.ForContext<MainWindow>();

            InitializeComponent();

            if (System.Drawing.SystemFonts.MenuFont != null) {
                folderFont = new Font( System.Drawing.SystemFonts.MenuFont.FontFamily, 9f, System.Drawing.FontStyle.Bold);
            } else {
                // Fallback font if MenuFont is null
                folderFont = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            }


            // Get the executable's directory (e.g., bin\Debug)
            string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (exeDir != null) {
                // Combine with the relative path to your image
                string imagePath = Path.Combine(exeDir, "Resources", "folder.ico");

                // Load the image 
                if (File.Exists(imagePath)) {
                    folderImage = Image.FromFile(imagePath); // Works with relative path
                } 

                imagePath = Path.Combine(exeDir, "Resources", "rdp.ico");
                if (File.Exists(imagePath)) {
                    connectionImage = Image.FromFile(imagePath); // Works with relative path
                } 
            }

            _viewModel = new MainViewModel();
            SetupSystemTray();
        }

        private void SetupSystemTray() {
            ContextMenuStrip _trayMenu = new ContextMenuStrip();

            _notifyIcon = new NotifyIcon {
                Icon = new Icon("Resources/rdp_tray.ico"),
                Text = "RDP Manager",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };


            _notifyIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    if (_trayMenu != null && _trayMenu.Visible ) {
                        _trayMenu.Visible = false;
                    } else {
                        ShowConnectionsMenu();
                    }
                }
            };

            RefreshTrayMenu();
        }

        private void RefreshTrayMenu( ) {
             if (_notifyIcon?.ContextMenuStrip == null) {
                 return;
             }
    
            _notifyIcon.ContextMenuStrip.Items.Clear();

            // Color definitions
            // var folderColor = Color.FromArgb(240, 240, 240);  // Light gray for folders
            var folderColor = Color.Blue;
            var folderBgColor = Color.Yellow;

            using (LogContext.PushProperty("Method", nameof(RefreshTrayMenu))) {

                _logger.Debug("Settings: {@Settings} ", _viewModel.Settings);
                _logger.Debug("MonitoredFolders: {MonitoredFolders} ", _viewModel.Settings.MonitoredFolders);

                foreach (var monitoredFolder in _viewModel.Settings.MonitoredFolders) {
                    _logger.Debug("monitoredFolder: {MonitoredFolders}:", monitoredFolder);
                    var folderConnections = _viewModel.Connections.Where(c => c.FilePath.StartsWith(monitoredFolder)).ToList();

                    // If no connection file found
                    if (!folderConnections.Any()) {
                        continue;
                    }


                    _logger.Debug("folderConnections: {List}:", folderConnections);







                    //****************************************************************************************************************
                    // Get connections directly in monitored folder (no subfolders)
                    //****************************************************************************************************************
                    var rootConnections = folderConnections.Where(c => Path.GetDirectoryName(c.FilePath) == monitoredFolder).OrderBy(c => c.DisplayName);

                    // Get connections in subfolders
                    var subfolderConnections = folderConnections.Where( c => Path.GetDirectoryName(c.FilePath) != monitoredFolder).ToList();

                    //************************************************************
                    // Add connections directly in monitored folder
                    //************************************************************
                    foreach (var connection in rootConnections) {
                        var menuItem = CreateConnectionMenuItem(connection);
                        _notifyIcon.ContextMenuStrip.Items.Add(menuItem);
                    }

                    // Group by subfolders
                    var subfolderGroups = subfolderConnections.GroupBy(c => {
                            var relativePath = c.FilePath.Substring(monitoredFolder.Length).TrimStart('\\');
                            return relativePath.Split('\\').First();
                            }).OrderBy(g => g.Key);

                    // Create a ToolStripMenuItem for each folder
                    foreach (var group in subfolderGroups) {
                        var folderItem = new ToolStripMenuItem(group.Key) {
                            // BackColor = folderBgColor,
                            ForeColor = folderColor,
                                      Font = folderFont,
                                      Image = folderImage
                        };

                        // For each connection, add to the folderItem
                        foreach (var connection in group.OrderBy(c => c.DisplayName)) {
                            var menuItem = CreateConnectionMenuItem(connection);
                            folderItem.DropDownItems.Add(menuItem);
                        }

                        // Add the folder to the Systemtray.menu
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
        }

        private ToolStripMenuItem CreateConnectionMenuItem(RdpConnection connection)
        {
            var menuItem = new ToolStripMenuItem(connection.IsFavorite ? $"★ {connection.DisplayName}" : connection.DisplayName);
            
            menuItem.MouseUp += (s, e) => {
                if (e.Button == MouseButtons.Right) {
                    _viewModel.ToggleFavoriteCommand.Execute(connection.FilePath);
                    RefreshTrayMenu();
                } else if (e.Button == MouseButtons.Left) {
                    _viewModel.LaunchCommand.Execute(connection.FilePath);
                }
            };

            if (connection.IsFavorite) {
                menuItem.Font = new Font(menuItem.Font, System.Drawing.FontStyle.Bold);
            }

            menuItem.BackColor = Color.White;
            menuItem.Image = connectionImage;

            return menuItem;
        }

        private void ShowConnectionsMenu() {
            if (_notifyIcon?.ContextMenuStrip != null) {
                // Using Control.MousePosition instead of Cursor.Position
                _notifyIcon.ContextMenuStrip.Show(Control.MousePosition);
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}
