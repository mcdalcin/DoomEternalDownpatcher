using System.Windows;
using System.Windows.Threading;
using Sentry;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private string APP_VERSION = "doom-eternal-downpatcher@0.3";

        private string SENTRY_SDK_URL = 
            "https://94f9011362c744f7a2f0bbcbec3ddc53@o506270.ingest.sentry.io/" +
            "5595765";


        public App() : base() {
            SentrySdk.Init(
                (options) => {
                    options.Dsn = new Dsn(SENTRY_SDK_URL);
                    options.Release = APP_VERSION;
                });
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(
            object sender, DispatcherUnhandledExceptionEventArgs e) {
            // TODO(xiae): Add in an option at startup to disable automatic error
            // reporting.
            SentrySdk.CaptureException(e.Exception);
            MessageBox.Show(
                "An unhandled exception has occurred and automatically been " +
                "reported. If you are unable to make progress, please request " +
                "help in the Modern DOOM Speedrunning discord.");
            e.Handled = false;
        }
    }
}