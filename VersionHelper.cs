using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RdpManager.Utilities {
    public static class VersionHelper {
        public static string GetApplicationName() {
            try {
                return Assembly.GetEntryAssembly()?.GetName().Name ?? "Application";
            } catch {
                return "Application";
            }
        }

        public static string GetShortVersion() {
            try {
                var version = Assembly.GetEntryAssembly()?.GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
            } catch {
                return "0.0.0";
            }
        }

        public static string GetFullVersionInfo() {
            try {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                var version = assembly.GetName().Version;

                // Extract changeset from ProductVersion (format: "1.0.0+abc12345")
                var changeset = versionInfo.ProductVersion?.Contains('+') == true
                    ? versionInfo.ProductVersion.Split('+')[1][..8]  // Take first 8 chars of hash
                    : "unknown";

                return $"Version: {version?.Major}.{version?.Minor}.{version?.Build}\n" +
                       $"Changeset: {changeset}\n" +
                       $"Build date: {File.GetLastWriteTime(assembly.Location):yyyy-MM-dd HH:mm}";
            } catch (Exception ex) {
                Debug.WriteLine($"Error getting version info: {ex}");
                return "Version information unavailable";
            }
        }

        public static string GetCopyrightInfo() {
            try {
                var versionInfo = FileVersionInfo.GetVersionInfo(
                    Assembly.GetEntryAssembly()?.Location ?? 
                    Assembly.GetExecutingAssembly().Location);

                return versionInfo.LegalCopyright ?? 
                      $"© {DateTime.Now.Year} {GetApplicationName()}";
            } catch {
                return $"© {DateTime.Now.Year}";
            }
        }

        public static DateTime GetBuildDate() {
            try {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                return File.GetLastWriteTime(assembly.Location);
            } catch {
                return DateTime.MinValue;
            }
        }
    }
}