using System.Windows;

using MessageBox = System.Windows.Forms.MessageBox;
using System.Windows.Threading;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() : base() {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(
            object sender, DispatcherUnhandledExceptionEventArgs e) {
            // TODO(xiae): Automate this error reporting to some discord webhook.
            MessageBox.Show(
                "An unhandled exception occured. Please report this to Xiae#5944 " +
                "on discord! " + e.Exception.ToString());
        }
    }
}