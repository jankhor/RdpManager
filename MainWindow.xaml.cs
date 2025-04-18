using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using Application = System.Windows.Application;
using RdpManager.ViewModels;  // Add this using directive
using RdpManager.Views;
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
        private Image? webImage=null;
        private Image? defaultScImage=null;

        private readonly ILogger _logger;

        public MainWindow() {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RdpManager");
            var     logFile = Path.Combine(appDataPath, "logs/rdpManager.log");

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .Enrich.FromLogContext()  // REQUIRED for LogContext properties
                .WriteTo.Console()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
                         outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}.{Method}: {Message:lj}{NewLine}{Exception}"
                ).CreateLogger();

            _logger = Log.ForContext<MainWindow>();

            _logger.Debug ("=====================================================================================");
            _logger.Debug ("===                                 Start                                         ===");
            _logger.Debug ("=====================================================================================");
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

                imagePath = Path.Combine(exeDir, "Resources", "web.ico");
                if (File.Exists(imagePath)) {
                    webImage = Image.FromFile(imagePath); // Works with relative path
                } 

                imagePath = Path.Combine(exeDir, "Resources", "shortcut.ico");
                if (File.Exists(imagePath)) {
                    defaultScImage = Image.FromFile(imagePath); // Works with relative path
                } 

            }

            _viewModel = new MainViewModel();
            SetupSystemTray();
        }

        //****************************************************************************************************************************
        //****************************************************************************************************************************
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

        //****************************************************************************************************************************
        //****************************************************************************************************************************
        private void RefreshTrayMenu( ) {
            if (_notifyIcon?.ContextMenuStrip == null) {
                 return;
            }
    
            _notifyIcon.ContextMenuStrip.Items.Clear();

            //******************************************
            // Build the Menu Structure
            //******************************************
            buildMenu();

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

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => {
                var aboutDialog = new AboutDialog();
                aboutDialog.Owner = this;
                aboutDialog.ShowInTaskbar = false; // For system tray consistency
                aboutDialog.ShowDialog();
            };
            _notifyIcon.ContextMenuStrip.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Current.Shutdown();
            _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        }

        //****************************************************************************************************************************
        //****************************************************************************************************************************
        private void buildMenu() {

            // Color definitions
            // var folderColor = Color.FromArgb(240, 240, 240);  // Light gray for folders
            var folderColor = Color.Blue;
            var folderBgColor = Color.Yellow;

            using (LogContext.PushProperty("Method", nameof(RefreshTrayMenu))) {

                _logger.Debug("Settings - {@Settings} ", _viewModel.Settings);
                _logger.Debug("MonitoredFolders - {@MonitoredFolders} ", _viewModel.Settings.MonitoredFolders);


                //*****************************************************************************************************************
                //* For each monitored folder in MonitoredFolders list
                //*****************************************************************************************************************
                foreach (var monitoredFolder in _viewModel.Settings.MonitoredFolders) {

                    //**************************************************************************
                    //* Single monitored folder, add the connections to to the root and add sub-folders
                    //**************************************************************************
                    if (_viewModel.Settings.MonitoredFolders.Count == 1) {
                        _logger.Debug("Single monitored folders add to root -  {@MonitoredFolders}", monitoredFolder);

                        //-------------------------------------------------------------------------------
                        //- Call AddConnectionToMenuRecursive, and provide null as the parent.  
                        //-  AddConnectionToMenuRecursive will check and add to the ContextMenuStrip
                        // -------------------------------------------------------------------------------
                        AddConnectionToMenuRecursive (null, monitoredFolder);
                    } 
                    //**************************************************************************
                    //* Two or more monitored folder, create a folder for each monitored folder
                    //* to group the connections under.
                    //**************************************************************************
                    else if (_viewModel.Settings.MonitoredFolders.Count > 1) {
                        string monitoredFolderName = Path.GetFileName(monitoredFolder);
                        _logger.Debug("Multiple monitored folders, group by folder name", monitoredFolderName);

                        var folderItem = new ToolStripMenuItem(monitoredFolderName) { Font = folderFont, Image = folderImage};

                        // -------------------------------------------------------------------------------
                        // Call AddConnectionToMenuRecursive, and provide the folderItem as the parent.
                        // -------------------------------------------------------------------------------
                        AddConnectionToMenuRecursive (folderItem, monitoredFolder);

                        _notifyIcon?.ContextMenuStrip?.Items.Add(folderItem);
                    }
                }
            }
        }

        private void AddConnectionToMenuRecursive (ToolStripMenuItem? parentMenu, String folder) {
            using (LogContext.PushProperty("Method", nameof(RefreshTrayMenu))) {

                _logger.Debug("****** Add connections ******* (" + folder + ")");

                //************************************************************
                // Any connections under the current folder
                //************************************************************
                var anyConnections = _viewModel.Connections.Where(c => c.FilePath.StartsWith(folder)).ToList();
                if (anyConnections.Count == 0) {
                    return;
                }

                var folderConnections = _viewModel.Connections.Where(c => Path.GetDirectoryName(c.FilePath) == folder).OrderBy(c => c.DisplayName).ToList();
                // _logger.Debug("----------------------------------------------------------------------------------------------------");
                // _logger.Debug("folderConnections {@Connections}", folderConnections);
                // _logger.Debug("----------------------------------------------------------------------------------------------------\n");

                //************************************************************
                // Add connections 
                //************************************************************
                ToolStripMenuItem? menuItem = null;
                foreach (var connection in folderConnections) {
                    menuItem = new ToolStripMenuItem(connection.IsFavorite ? $"★ {connection.DisplayName}" : connection.DisplayName);

                    var filePath = connection.FilePath;

                    if (filePath.EndsWith(".rdp")) {
                        menuItem.Image = connectionImage;
                    } else if (filePath.EndsWith(".url")) {
                        menuItem.Image = webImage;
                    } else if (filePath.EndsWith(".lnk")) {
                        // Set the default icon
                        menuItem.Image = defaultScImage;

                        try {
                            _logger.Debug ("** Loading shortcut icon " + filePath);
                            var icon = ShortcutParser.ExtractFileIcon (filePath);
                            if (icon != null) {
                                menuItem.Image = icon.ToBitmap();
                            }
                        } catch (Exception ex) {
                            _logger.Error ($"Icon extraction failed:", ex);
                        }

                    }

                    if (connection.IsFavorite) {
                        menuItem.Font = new Font(menuItem.Font, System.Drawing.FontStyle.Bold);
                    }

                    menuItem.MouseUp += (s, e) => {
                        if (e.Button == MouseButtons.Right) {
                            _viewModel.ToggleFavoriteCommand.Execute(connection.FilePath);
                            RefreshTrayMenu();
                        } else if (e.Button == MouseButtons.Left) {
                            _viewModel.LaunchCommand.Execute(connection.FilePath);
                        }
                    };

                    if (parentMenu != null) {
                        parentMenu.DropDownItems.Add(menuItem);
                    } else {
                        _notifyIcon?.ContextMenuStrip?.Items.Add(menuItem);
                    }
                }

                //************************************************************
                // Process subfolders
                //************************************************************
                foreach (string subFolder in Directory.GetDirectories(folder)) {
                    menuItem = new ToolStripMenuItem( Path.GetFileName(subFolder));
                    anyConnections = _viewModel.Connections.Where(c => c.FilePath.StartsWith(folder)).ToList();
                    if (anyConnections.Count == 0) {
                        continue;
                    }

                    _logger.Debug("     subFolder - "+ subFolder);

                    menuItem = new ToolStripMenuItem( Path.GetFileName(subFolder));
                    menuItem.Image = folderImage;

                    if (parentMenu != null) {
                        parentMenu.DropDownItems.Add(menuItem);
                    } else {
                        _notifyIcon?.ContextMenuStrip?.Items.Add(menuItem);
                    }

                    AddConnectionToMenuRecursive (menuItem, subFolder);
                }
            }
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
