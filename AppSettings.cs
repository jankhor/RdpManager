using System.Collections.Generic;

namespace RdpManager
{
    public class AppSettings
    {
        public List<string> MonitoredFolders { get; set; } = new()
        {
            // System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            // System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\RDWeb"
        };
        
        public bool RunAtStartup { get; set; } = true;
        public bool ShowRecentFirst { get; set; } = true;
        public int MaxRecentItems { get; set; } = 10;
    }
}
