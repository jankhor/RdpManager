using System.Windows;

namespace RdpManager.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.SettingsViewModel();
        }
    }
}
