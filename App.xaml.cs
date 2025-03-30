using System.Windows; // For WPF Application
using System.Windows.Forms; // For NotifyIcon (but we won't use Forms.Application)

namespace RdpManager
{
    // Fully qualify which Application we're inheriting from
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Single instance check
            var mutex = new System.Threading.Mutex(true, "RdpManager", out bool createdNew);
            if (!createdNew)
            {
                // Fully qualify which Application's Current we're using
                System.Windows.Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}
