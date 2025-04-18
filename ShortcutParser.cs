using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;  // Added for StringBuilder
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

    static string? GetShortcutTarget(string shortcutPath) {
        try {
            dynamic shell = Interaction.CreateObject("WScript.Shell");
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            if (shortcut != null) {
                return shortcut.TargetPath as string;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error reading shortcut: " + ex.Message);
        }

        return null;
    }

    /* Get icron from a shortcut file
     * 
     */
    public static Icon? ExtractFileIcon(string scLnkFilePath) {
        ILogger _logger = Log.ForContext(typeof(ShortcutParser));

        if (_iconCache.TryGetValue(scLnkFilePath, out var cachedIcon)) {
            return cachedIcon;
        }

        _logger.Debug($"Shortcut LNK file ${scLnkFilePath}");

        if (!File.Exists(scLnkFilePath)) {
            _logger.Error($"ExtractFileIcon - Shortcut not found: {scLnkFilePath}");
            return null;
        }

        string? scTargetPath = GetShortcutTarget (scLnkFilePath);

        _logger.Debug($"    Shortcut points to ${scTargetPath}");


        if (string.IsNullOrEmpty(scTargetPath)) {
            _logger.Error("Null file path provided");
            return null;
        }

        try {
            if (!File.Exists(scTargetPath)) {
                _logger.Error($"ExtractFileIcon - File not found: {scTargetPath}");
                return null;
            }

            Icon? icon = Icon.ExtractAssociatedIcon(scTargetPath);

            if (icon == null) {
                _logger.Error($"ExtractFileIcon - Failed to create Icon from handle for: {scTargetPath}");
                return null;
            }

            // Clone the icon because the original needs to stay alive
            var clonedIcon = (Icon)icon.Clone();

            if (clonedIcon != null) {
                _iconCache[scLnkFilePath] = clonedIcon;
            }

        _logger.Debug($"    Shortcut Icon FOUND ${scLnkFilePath}");
            return clonedIcon;
        } catch (Exception ex) {
            _logger.Error($"ExtractFileIcon - Icon extraction failed for: {ex.Message}");
        }

        return null;
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

