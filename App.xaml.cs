using System.Windows;

namespace RdpManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var mutex = new System.Threading.Mutex(true, "RdpManager", out bool createdNew);
            if (!createdNew)
            {
                Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }
    }
}
