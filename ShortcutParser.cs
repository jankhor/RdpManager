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

    private static ILogger _logger = null;

    //********************************************************************************
    //* Return the Shotcut targetPath and custom iconLocation
    //********************************************************************************
    static (string? target, string? iconLocation) GetShortcutTargetAndIcon(string shortcutPath) {
        try {
            dynamic shell = Interaction.CreateObject("WScript.Shell");
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            string targetPath = shortcut.TargetPath as string;
            string iconLocation = shortcut.IconLocation as string;
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
        if (string.IsNullOrEmpty(iconLocation)) {
            _logger.Debug($"  ExtractIconFromCustomLocation - iconLocation is nullOrEmpty : {iconLocation}");
            return null;
        }

        string[] parts = iconLocation.Split(',');
        string iconFilePath = parts[0];
        int index = parts.Length > 1 && int.TryParse(parts[1], out int idx) ? idx : 0;

        // 👇 Expand environment variables here
        iconFilePath = Environment.ExpandEnvironmentVariables(iconFilePath);


        if (!File.Exists(iconFilePath)) {
            _logger.Error($"  ExtractIconFromCustomLocation - iconFilePath not found: {iconFilePath}");
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

    //********************************************************************************
    //* Extract Icon from the shotcut targetPath
    //********************************************************************************
    static Icon? ExtractIconFromTargetPath (string? scTargetPath) {
        // 👇 Expand environment variables here
        scTargetPath = Environment.ExpandEnvironmentVariables(scTargetPath);

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

        
    //********************************************************************************
    //* Get icron from a shortcut file
    //********************************************************************************
    public static Icon? ExtractIconFromShortcut (string shortcutPath) {
        if (_logger == null) {
            _logger = Log.ForContext(typeof(ShortcutParser));
        }

        //**** If already in cache, return the cached icon
        if (_iconCache.TryGetValue(shortcutPath, out var cachedIcon)) {
            return cachedIcon;
        }

        _logger.Debug($"Shortcut LNK file ${shortcutPath}");

        if (!File.Exists(shortcutPath)) {
            _logger.Error($"  ExtractIconFromShortcut - Shortcut not found: {shortcutPath}");
            return null;
        }

        (string? scTargetPath, string? iconLocation) = GetShortcutTargetAndIcon(shortcutPath);

        _logger.Debug ("    Shortcut points to: " + scTargetPath);
        _logger.Debug ("    Icon location: " + iconLocation);


        try {
            // Try to get the Icon from the Custom file
            Icon? icon = ExtractIconFromCustomLocation (iconLocation);

            // If still null, try to get from the targetPath
            if (icon == null) {
                icon = ExtractIconFromTargetPath (scTargetPath);
            }

            if (icon == null) {
                return icon;
            }

            // Clone the icon because the original needs to stay alive
            var clonedIcon = (Icon)icon.Clone();

            if (clonedIcon != null) {
                _iconCache[shortcutPath] = clonedIcon;
            }

            _logger.Debug($"    Shortcut Icon FOUND ${shortcutPath}");
            return clonedIcon;
        } catch (Exception ex) {
            _logger.Error($"ExtractFileIcon - Icon extraction failed for: {ex.Message}");
            return null;
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

