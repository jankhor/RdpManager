using System.Windows; // For WPF Application
using System.Windows.Forms; // For NotifyIcon (but we won't use Forms.Application)
using System.IO;
using System.Reflection;

namespace RdpManager
{
    // Fully qualify which Application we're inheriting from
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            // Set the current working directory to where the executable is, for the executable to
            // be invoked from any folder and still resolve all the DLL.
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

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
