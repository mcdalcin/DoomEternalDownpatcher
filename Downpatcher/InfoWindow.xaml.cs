using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window {
        public InfoWindow() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            // Open up the hyperlink in a browser.
            ProcessStartInfo processStartInfo = 
                new ProcessStartInfo(e.Uri.AbsoluteUri);
            processStartInfo.UseShellExecute = true;
            Process.Start(processStartInfo);
            e.Handled = true;
        }
    }
}
