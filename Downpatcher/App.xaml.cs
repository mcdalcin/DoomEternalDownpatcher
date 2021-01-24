using System.Windows;
using System.Windows.Threading;
using Sentry;

using Settings = Downpatcher.Properties.Settings;

namespace Downpatcher {
    public partial class App : Application {
        public static readonly string APP_VERSION = "1.1";

        private const string SENTRY_SDK_URL = 
            "https://94f9011362c744f7a2f0bbcbec3ddc53@o506270.ingest.sentry.io/" +
            "5595765";

        public App() : base() {
            SentrySdk.Init(
                (options) => {
                    options.Dsn = SENTRY_SDK_URL;
                    options.Release = "doom-eternal-downpatcher@" + APP_VERSION;
                });
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(
            object sender, DispatcherUnhandledExceptionEventArgs e) {
            string message = 
                "An unhandled exception has occurred. If you are unable to make " +
                "progress, please request help in the Modern DOOM Speedrunning " +
                "discord.";
            if (Settings.Default.AutomaticallyReportExceptions) {
                message += " The exception has automatically been reported.";
                SentrySdk.CaptureException(e.Exception);
            } else {
                message += " Your exception is: " + e.Exception.ToString();
            }
            MessageBox.Show(message);
            e.Handled = false;
        }
    }
}