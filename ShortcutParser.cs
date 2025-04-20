using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;  // Added for StringBuilder
using System.Text.RegularExpressions;
using Microsoft.VisualBasic; // Needed for Interaction.CreateObject
using Serilog;
using Serilog.Sinks.File;  // Add this for RollingInterval
using Serilog.Context;

public static class ShortcutParser {


    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(
        IntPtr hInst,
        string lpszExeFileName,
        int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static readonly ConcurrentDictionary<string, Icon?> _iconCache = new();

    private static readonly ILogger _logger = Log.ForContext(typeof(ShortcutParser));

    //********************************************************************************
    //* Return the Shotcut targetPath and custom iconLocation
    //********************************************************************************
    static (string? target, string? iconLocation) GetShortcutTargetAndIcon(string shortcutPath) {
        try {
            dynamic shell = Interaction.CreateObject("WScript.Shell");
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            string? targetPath = shortcut.TargetPath as string;
            string? iconLocation = shortcut.IconLocation as string;
            return (targetPath, iconLocation);
        } catch (Exception ex) {
            Console.WriteLine("Error reading shortcut: " + ex.Message);
            return (null, null);
        }
    }

    //********************************************************************************
    //* Extract Icon from custom iconLocation ("path,index")
    //********************************************************************************
    static Icon? ExtractIconFromCustomLocation(string? iconLocation) {
        using (LogContext.PushProperty("Method", nameof(ExtractIconFromCustomLocation))) {
            if (string.IsNullOrEmpty(iconLocation)) {
                _logger?.Debug($"  ExtractIconFromCustomLocation - iconLocation is nullOrEmpty : {iconLocation}");
                return null;
            }

            string[] parts = iconLocation.Split(',');
            string iconFilePath = parts[0];
            int index = parts.Length > 1 && int.TryParse(parts[1], out int idx) ? idx : 0;

            // 👇 Expand environment variables here
            iconFilePath = Environment.ExpandEnvironmentVariables(iconFilePath ?? string.Empty);


            if (!File.Exists(iconFilePath)) {
                _logger?.Error($"  ExtractIconFromCustomLocation - iconFilePath not found: {iconFilePath}");
                return null;
            }

            // Uses Windows API to extract icons at specific index
            var hModule = IntPtr.Zero;
            var hIcon = ExtractIcon(IntPtr.Zero, iconFilePath, index);

            if (hIcon != IntPtr.Zero) {
                _logger.Debug($"  ExtractIconFromCustomLocation - Found custom icon: {iconFilePath}");
                return Icon.FromHandle(hIcon);
            } else {
                _logger.Debug($"  ExtractIconFromCustomLocation - Custom icon not found: {iconFilePath}");
                return null;
            }
        }
    }

    //********************************************************************************
    //* Extract Icon from the shotcut targetPath
    //********************************************************************************
    static Icon? ExtractIconFromTargetPath (string? scTargetPath) {
        using (LogContext.PushProperty("Method", nameof(ExtractIconFromTargetPath))) {
            // 👇 Expand environment variables here
            scTargetPath = Environment.ExpandEnvironmentVariables(scTargetPath ?? string.Empty);

            if (!File.Exists(scTargetPath)) {
                _logger.Error($"  ExtractIconFromTargetPath - File not found: {scTargetPath}");
                return null;
            }

            Icon? icon = Icon.ExtractAssociatedIcon(scTargetPath);

            if (icon == null) {
                _logger.Error($"ExtractIconFromTargetPath - Failed to create Icon from handle for: {scTargetPath}");
                return null;
            }

            return icon;
        }
    }

        
    //********************************************************************************
    //* Get icron from a shortcut file
    //********************************************************************************
    public static Icon? ExtractIconFromShortcut (string shortcutPath) {
        using (LogContext.PushProperty("Method", nameof(ExtractIconFromShortcut))) {
            //**** If already in cache, return the cached icon
            if (_iconCache.TryGetValue(shortcutPath, out var cachedIcon)) {
                return cachedIcon;
            }

            _logger.Debug($"Shortcut file ${shortcutPath}");

            if (!File.Exists(shortcutPath)) {
                _logger.Error($"  ExtractIconFromShortcut - Shortcut not found: {shortcutPath}");
                return null;
            }

            (string? scTargetPath, string? iconLocation) = GetShortcutTargetAndIcon(shortcutPath);

            string extension = Path.GetExtension(shortcutPath).ToLowerInvariant();

            try {

                Icon? icon = null;

                if (extension == ".rdp") { 
                    _logger.Debug ("    RDP Shortcut points to: " + shortcutPath);
                    icon = ExtractIconFromTargetPath (shortcutPath);
                } else if (extension == ".lnk") {
                    _logger.Debug ("    LNK Shortcut points to: " + scTargetPath);
                    _logger.Debug ("    LNK Icon location: " + iconLocation);

                    // Try to get the Icon from the Custom file
                    icon = ExtractIconFromCustomLocation (iconLocation);

                    // If still null, try to get from the targetPath
                    if (icon == null) {
                        icon = ExtractIconFromTargetPath (scTargetPath);
                    }
                } else if (extension == ".url") {
                    (string? iconFile, int iconIndex, string? url) = ParseUrlShortcut(shortcutPath);

                    _logger.Debug("    URL Shortcut points to: " + url);
                    _logger.Debug("    URL IconFile: " + iconFile);
                    _logger.Debug("    URL IconIndex: " + iconIndex);


                    if (!string.IsNullOrEmpty(iconFile)) {
                        icon = ExtractIconFromCustomLocation($"{iconFile},{iconIndex}");
                    }

                    // If no iconFile or icon not found, try resolving from protocol or associated app
                    if (icon == null && !string.IsNullOrEmpty(url)) {
                        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
                            if (!string.IsNullOrEmpty(uri.Scheme)) {
                                // Try get associated executable for custom protocol (e.g., onenote:)
                                string? handlerExe = GetApplicationForProtocol(uri.Scheme);
                                if (!string.IsNullOrEmpty(handlerExe)) {
                                    _logger?.Debug($"    HandlerExe {handlerExe}");
                                    icon = ExtractIconFromTargetPath(handlerExe);
                                } else {
                                }
                            }
                        } else {
                            // Fallback: try to get icon from the raw string if it's a local file or UNC path
                            icon = ExtractIconFromTargetPath(url);
                        }
                    }
                }

                if (icon == null) {
                    return icon;
                }

                // Clone the icon because the original needs to stay alive
                var clonedIcon = (Icon)icon.Clone();

                if (clonedIcon != null) {
                    _iconCache[shortcutPath] = clonedIcon;
                }

                _logger?.Debug($"    Shortcut Icon FOUND ${shortcutPath}");
                return clonedIcon;
            } catch (Exception ex) {
                _logger.Error($"ExtractFileIcon - Icon extraction failed for: {ex.Message}");
                return null;
            }
        }
    }

    private static string? GetApplicationForProtocol(string scheme) {
        using (LogContext.PushProperty("Method", nameof(GetApplicationForProtocol))) {
            _logger?.Debug($"    **** GetApplicationForProtol: {scheme} ****");
            try {
                using (var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($"{scheme}\\shell\\open\\command")) {
                    if (key != null) {
                        _logger?.Debug($"    key: {key}");
                        var value = key.GetValue("") as string;

                        if (!string.IsNullOrEmpty(value)) {
                            // Extract executable path (strip quotes and args)
                            _logger?.Debug($"    value: {value}");
                            // var match = System.Text.RegularExpressions.Regex.Match(value, "\"([^\"]+\\.exe)\"");
                            var match = System.Text.RegularExpressions.Regex.Match(value, "\"([^\"]+\\.exe)\"", RegexOptions.IgnoreCase);
                            

                            if (match.Success) {
                                _logger?.Debug($"    match.Success: {match.Groups[1].Value}");
                                return match.Groups[1].Value;
                            } else {
                                _logger?.Debug($"    Not match");
                            }
                        } else {
                            _logger?.Debug($"    value was null");
                        }

                    } else {
                        _logger?.Debug($"    key lookup is null");
                    }
                }
            } catch (Exception ex) {
                _logger?.Error($"Failed to get application for protocol '{scheme}': {ex.Message}");
            }
            return null;
        }
    }
    
    private static (string? iconFile, int iconIndex, string? url) ParseUrlShortcut(string urlShortcutPath) {
        string? iconFile = null;
        int iconIndex = 0;
        string? url = null;

        using (LogContext.PushProperty("Method", nameof(ParseUrlShortcut))) {
            try {
                foreach (var line in File.ReadAllLines(urlShortcutPath)) {
                    if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase)) {
                        iconFile = line.Substring("IconFile=".Length).Trim();
                        iconFile = Environment.ExpandEnvironmentVariables(iconFile);
                    } else if (line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase)) {
                        int.TryParse(line.Substring("IconIndex=".Length).Trim(), out iconIndex);
                    } else if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase)) {
                        url = line.Substring("URL=".Length).Trim();
                    }
                }
            } catch (Exception ex) {
                _logger.Error($"ParseUrlShortcut - Failed to parse {urlShortcutPath}: {ex.Message}");
            }

            return (iconFile, iconIndex, url);
        }
    }

    public static void ClearCache() {
        foreach (var icon in _iconCache.Values) {
            icon?.Dispose();
        }
        _iconCache.Clear();
    }

    /* [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { } */
}
