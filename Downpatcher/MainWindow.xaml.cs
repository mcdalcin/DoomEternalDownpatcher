using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Forms;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using System.Windows.Navigation;

namespace Downpatcher {
    public partial class MainWindow : Window {
        private const string DEPOT_DOWNLOADER_RELEASE_URL =
            "https://api.github.com/repos/SteamRE/DepotDownloader/releases";
        private const string DEPOT_DOWNLOADER_AUTH_2FA_REGEX_STRING =
            "Please enter your 2 factor auth code from your authenticator app";
        private const string DEPOT_DOWNLOADER_AUTH_CODE_REGEX_STRING =
            "Please enter the authentication code sent to your email address";

        private const string DOOM_ETERNAL_EXE_STRING = "DOOMEternalx64vk.exe";
        private const string DOOM_ETERNAL_DATA_BASE_URL =
            "https://raw.githubusercontent.com/mcdalcin/DoomEternalDownpatcher"
                + "/master/data/";
        private const string DOOM_ETERNAL_VERSION_URL =
            DOOM_ETERNAL_DATA_BASE_URL + "versions.json";

        private const string DOWNPATCHER_RELEASE_URL =
            "https://api.github.com/repos/mcdalcin/DoomEternalDownpatcher/releases";


        private string _doomEternalPath = "";
        private string _doomEternalDetectedVersion = "";

        private volatile string _doomEternalDownpatchFolder = "";
        private volatile string _depotDownloaderInstallPath = "";

        private DoomVersions _availableVersions = new DoomVersions();

        private ConsoleContent _console;

        private volatile Process _depotDownloaderProcess;
        private Object _depotDownloaderProcessLock = new Object();

        public MainWindow() {
            InitializeComponent();
            _console = new ConsoleContent(scroller);
            DataContext = _console;
            CheckForUpdates();
            _doomEternalPath = InitializeDoomRootPath();
            _doomEternalDetectedVersion = InitializeDoomVersion();
            InitializeDoomDownpatchVersions();
            InitializeDepotDownloader();
            cbReportErrors.IsChecked = 
                Properties.Settings.Default.AutomaticallyReportExceptions;

            // Initialize the default DOOM Eternal downpatch folder.
            _doomEternalDownpatchFolder = 
                Directory.GetCurrentDirectory() + @"\DOWNPATCH_FILES";
            lSelectedFolder.Content = new Run(_doomEternalDownpatchFolder);
        }

        /** Checks for updates to the downpatcher and alerts the user if found. */
        private void CheckForUpdates() {
            string jsonString;
            try {
                WebClient webClient = new WebClient();
                webClient.Headers.Add("User-Agent: Other");
                jsonString = webClient.DownloadString(DOWNPATCHER_RELEASE_URL);
            } catch (WebException e) {
                _console.Output(
                    "ERROR: Unable to check for Downpatcher updates. Please " +
                    "make sure there is an active network and this program is " +
                    "not being blocked by your firewall or antivirus.");
                return;
            }
            dynamic json = JsonConvert.DeserializeObject(jsonString);
            
            if (json.Count == 0) {
                _console.Output(
                    "ERROR: No releases found for downpatcher while checking " +
                    "for updates.");
                return;
            }

            // Get the version tag of the latest release.
            string latestVersionTag = json[0].tag_name;

            var latestVersion = new Version(latestVersionTag);
            var currentVersion = new Version(App.APP_VERSION);

            if (latestVersion.CompareTo(currentVersion) > 0) {
                tbUpdateNotification.Visibility = Visibility.Visible;
            }
        }

        /** 
         * Returns the DOOM Eternal root folder path or an empty string if not found.
         */
        private string InitializeDoomRootPath() {
            // Get steam base path from registry.
            RegistryKey localKey =
                RegistryKey.OpenBaseKey(
                    RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam");
            if (localKey == null) {
                return "";
            }
            string steamPath = localKey.GetValue("InstallPath").ToString();
            string doomEternalPath = steamPath + @"\steamapps\common\DOOMEternal";
            // Check that the DOOM Eternal folder exists.
            return ValidateDoomRootPath(doomEternalPath);
        }

        private string ValidateDoomRootPath(string path) {
            // Check that the DOOM Eternal folder exists.
            tbRootPath.Inlines.Clear();
            if (Directory.Exists(path)) {
                _console.Output("Successfully found DOOM Eternal root folder!");
                tbRootPath.Inlines.Add(new Bold(new Run("Root folder found: ")) {
                    Foreground =
                        new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#C7F464"))
                });
                tbRootPath.Inlines.Add(new Run(path));
                return path;
            } else {
                _console.Output(
                    "ERROR: Could not find DOOM Eternal root folder! Please " +
                    "select it manually.");
                spRootPath.Visibility = Visibility.Hidden;
                spSelectDoomFolder.Visibility = Visibility.Visible;
                return "";
            }
        }

        /** 
         * Returns the installed DOOM Eternal version or an empty string if unable to
         * detect it. _doomEternalPath should be set to the DOOM Eternal root path
         * before calling this function.
         */
        private string InitializeDoomVersion() {
            string doomEternalExePath =
                _doomEternalPath + @"\" + DOOM_ETERNAL_EXE_STRING;

            if (!File.Exists(doomEternalExePath)) {
                _console.Output(
                    "ERROR: DOOM Eternal executable not found. Please select " +
                    "your DOOM Eternal root folder containing the DOOM Eternal " +
                    "executable (DOOMEternalx64vk.exe).");
                spRootPath.Visibility = Visibility.Hidden;
                spSelectDoomFolder.Visibility = Visibility.Visible;
                return "";
            }

            long exeSize = new FileInfo(doomEternalExePath).Length;
            string doomVersion = DetermineDoomVersion(exeSize);

            tbVersion.Inlines.Clear();
            if (doomVersion.Length != 0) {
                _console.Output(
                    "Successfully detected installed DOOM Eternal version: "
                        + doomVersion);
                tbVersion.Inlines.Add(
                    new Bold(new Run("Installed DOOM Eternal version: ") {
                        Foreground =
                            new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#C7F464"))
                    }));
                tbVersion.Inlines.Add(new Run(doomVersion));
                spRootPath.Visibility = Visibility.Visible;
                spSelectDoomFolder.Visibility = Visibility.Hidden;
            } else {
                _console.Output(
                    "ERROR: Unable to detect installed DOOM Eternal version. " +
                    "Please report this error to the speedrunning discord!");
                spRootPath.Visibility = Visibility.Visible;
                spSelectDoomFolder.Visibility = Visibility.Hidden;
                return "";
            }
            return doomVersion;
        }

        /** 
         * Ensure the most updated version of DepotDownloader is installed and
         * ready to use.
         */
        private void InitializeDepotDownloader() {
            // Check for latest depot downloader release located in current working 
            // directory.
            using (var webClient = new WebClient()) {
                webClient.Headers.Add("User-Agent: Other");
                string jsonString;
                try {
                    jsonString =
                        webClient.DownloadString(DEPOT_DOWNLOADER_RELEASE_URL);
                } catch (WebException e) {
                    _console.Output(
                        "ERROR: Unable to download DepotDownloader metadata. Please " +
                        "make sure there is an active network and this program is " +
                        "not being blocked by your firewall or antivirus.");
                    return;
                }
                dynamic json = JsonConvert.DeserializeObject(jsonString);
                if (json.Count == 0) {
                    _console.Output("ERROR: No version of depot downloader found.");
                    return;
                }
                string name = json[0].name;
                string fileName = json[0].assets[0].name;
                string downloadUrl = json[0].assets[0].browser_download_url;
                _depotDownloaderInstallPath =
                    Directory.GetCurrentDirectory() + @"\"
                        + Path.GetFileNameWithoutExtension(fileName);
                // If latest DepotDownloader is not already installed, download and 
                // unzip it.
                if (!Directory.Exists(_depotDownloaderInstallPath)) {
                    _console.Output(
                        "New DepotDownloader version detected. Installing " + name
                            + ".");
                    _console.Output("Downloading " + downloadUrl);
                    try {
                        webClient.DownloadFile(
                            downloadUrl,
                            Directory.GetCurrentDirectory() + @"\" + fileName);
                    } catch (WebException e) {
                        _console.Output(
                            "ERROR: Unable to download DepotDownloader. Please " +
                            "make sure there is an active network and this " +
                            "program is not being blocked by your firewall or " +
                            "antivirus.");
                    }
                    _console.Output("Unpacking downloaded files.");
                    ZipFile.ExtractToDirectory(
                        fileName, _depotDownloaderInstallPath);
                    _console.Output("Successfully unpacked DepotDownloader!");
                }
                _console.Output(name + " installed to current directory.");
            }
        }

        private void InitializeDoomDownpatchVersions() {
            // For now, only include versions less than our current version.
            int count = 0;
            foreach (var version in _availableVersions.versions) {
                if (_doomEternalDetectedVersion.Equals(version.name)) {
                    break;
                }
                cbDownpatchVersion.Items.Add(version.name);
                count++;
            }
            _console.Output(
                "There are " + count + " available downpatch versions. Please pick "
                    + "one above.");
        }

        private void UpdateDownpatcherButtons() {
            UpdateStartDownpatcherButton();
            UpdateCancelDownpatcherButton();
            UpdateRequiredNotifiers();
            spInProgress.Visibility = 
                _depotDownloaderProcess != null 
                    ? Visibility.Visible 
                    : Visibility.Hidden;
        }

        private void UpdateRequiredNotifiers() {
            tbUsernameRequired.Visibility =
                tbUsername.Text.Length == 0
                    ? Visibility.Visible
                    : Visibility.Hidden;
            tbPasswordRequired.Visibility =
                pbPassword.Password.Length == 0
                    ? Visibility.Visible
                    : Visibility.Hidden;
            tbVersionRequired.Visibility =
                cbDownpatchVersion.SelectedItem == null
                    ? Visibility.Visible
                    : Visibility.Hidden;
            tbRequiredNotification.Visibility =
                tbUsernameRequired.Visibility == Visibility.Hidden
                    && tbPasswordRequired.Visibility == Visibility.Hidden
                    && tbVersionRequired.Visibility == Visibility.Hidden
                        ? Visibility.Hidden
                        : Visibility.Visible;
        }

        private void UpdateStartDownpatcherButton() {
            bStartDownpatcher.IsEnabled =
                cbDownpatchVersion.SelectedItem != null &&
                tbUsername.Text.Length != 0 &&
                pbPassword.Password.Length != 0 &&
                _doomEternalDetectedVersion.Length != 0 &&
                _doomEternalDownpatchFolder.Length != 0 &&
                _depotDownloaderInstallPath.Length != 0 &&
                _depotDownloaderProcess == null;
        }

        private void UpdateCancelDownpatcherButton() {
            bCancelDownpatcher.IsEnabled = _depotDownloaderProcess != null;
        }

        /** 
         * Returns the file list for the specified version in an array of strings. 
         */
        private string[] GetFileList(string versionName) {
            using (var webClient = new WebClient()) {
                string files;
                try {
                    files = webClient.DownloadString(
                        DOOM_ETERNAL_DATA_BASE_URL + versionName + ".txt");
                } catch (WebException e) {
                    _console.Output(
                        "ERROR: Unable to download filelist for " + versionName +
                        ". Please make sure there is an active network and this " +
                        "program is not being blocked by your firewall or " +
                        "antivirus.");
                    return new string[0];
                }
                // Split files on each newline.
                return files.Split(
                    new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private string DetermineDoomVersion(long exeSize) {
            using (var webClient = new WebClient()) {
                string json;
                try {
                    json = webClient.DownloadString(DOOM_ETERNAL_VERSION_URL);

                } catch (WebException e) {
                    _console.Output(
                        "ERROR: Unable to download versioning metadata. Please " +
                        "make sure there is an active network and this program is " +
                        "not being blocked by your firewall or antivirus.");
                    return "";
                }
                _availableVersions =
                    JsonConvert.DeserializeObject<DoomVersions>(json);
                foreach (var version in _availableVersions.versions) {
                    if (version.size == exeSize) {
                        return version.name;
                    }
                }
            }

            return "";
        }

        private void ExecuteDepotDownloads(
            string[] depotIds,
            string[] manifestIds,
            string fileListPath,
            string username,
            string password) {
            
            if (depotIds.Length != manifestIds.Length) {
                _console.Output(
                    "ERROR: Unable to execute depot downloads. Non-matching " +
                    "number of depots and manifests (" + depotIds.Length + ", " + 
                    manifestIds.Length + ")");
                return;
            }

            // TODO(xiae): Starting up a command prompt to run a dotnet dll from
            // a dotnet app seems like a circular process. Can we do better and call
            // the dotnet dll ourselves?

            ProcessStartInfo processInfo;

            string command =
                "dotnet.exe " + _depotDownloaderInstallPath
                + @"\DepotDownloader.dll" 
                + " -app 782330"
                + " -username " + username
                + " -password " + password
                + " -remember-password"
                + " -max-servers 16"
                + " -max-downloads 16"
                + " -validate"
                + " -filelist \"" + fileListPath
                + "\" -dir \"" + _doomEternalDownpatchFolder + "\"";

            command += " -depot";
            foreach (string depotId in depotIds) {
                command += " " + depotId;
            }

            command += " -manifest";
            foreach (string manifestId in manifestIds) {
                command += " " + manifestId;
            }

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // Redirect command prompt output.
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardInput = true;

            lock (_depotDownloaderProcessLock) {
                _depotDownloaderProcess = new Process();

                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => UpdateDownpatcherButtons());

                // Route errors and exit behavior.
                _depotDownloaderProcess.ErrorDataReceived +=
                    (object sender, DataReceivedEventArgs e) => {
                        HandleDepotDownloaderOutput(e.Data);
                    };

                // DepotDownloader may have an interaction prompt for authentication,
                // therefore we'll need to read in the output ourselves.
                Thread readStandardOutput = new Thread(() => {

                    StringBuilder output = new StringBuilder();
                    char character;

                    Process p = _depotDownloaderProcess;
                    try {
                        while (_depotDownloaderProcess != null
                               && p != null
                               && (character = (char)p.StandardOutput.Read()) >= 0) {
                            // Accumulate buffer until newline or authentication 
                            // interaction prompt.
                            bool requiresAuth =
                                output.ToString().Contains(
                                    DEPOT_DOWNLOADER_AUTH_2FA_REGEX_STRING)
                                || output.ToString().Contains(
                                    DEPOT_DOWNLOADER_AUTH_CODE_REGEX_STRING);
                            if (character == '\n' || requiresAuth) {
                                HandleDepotDownloaderOutput(output.ToString());
                                output.Clear();
                            } else {
                                output.Append(character);
                            }
                        }
                    } catch (Exception e) {
                        // Ignore.
                    }
                });

                _depotDownloaderProcess.StartInfo = processInfo;
                _depotDownloaderProcess.Start();
                readStandardOutput.Start();
                _depotDownloaderProcess.BeginErrorReadLine();
                _depotDownloaderProcess.WaitForExit();
            }

            // Notify the application that the process has ended.
            System.Windows.Application.Current.Dispatcher.Invoke(
                () => KillDepotDownloaderProcess());
        }

        private void HandleDepotDownloaderOutput(string output) {
            if (output == null || output == "") {
                return;
            }
            // Invoke console output on the UI thread and strip off any stray newline
            // characters.
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
                _console.Output("DepotDownloader>> " + output.Replace('\n', '\0')));

            bool requiresAuth =
                output.Contains(DEPOT_DOWNLOADER_AUTH_2FA_REGEX_STRING)
                || output.Contains(DEPOT_DOWNLOADER_AUTH_CODE_REGEX_STRING);
            if (requiresAuth) {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    AuthenticationDialog authenticationDialog = 
                        new AuthenticationDialog();
                    if (authenticationDialog.ShowDialog() == true) {
                        _console.Output(
                            "Using authentication code: " 
                                + authenticationDialog.AuthCode);

                        StreamWriter sw = _depotDownloaderProcess.StandardInput;
                        sw.WriteLine(authenticationDialog.AuthCode);
                        sw.Flush();
                    } else {
                        _console.Output(
                            "Authentication code not entered. Shutting down "
                                + "DepotDownloader.");
                        KillDepotDownloaderProcess();
                    }
                });
            }
        }

        /** Must be called from the UI-thread. */
        private void KillDepotDownloaderProcess() {
            if (_depotDownloaderProcess != null 
                && !_depotDownloaderProcess.HasExited) {
                _depotDownloaderProcess.Kill(true);
                _depotDownloaderProcess.Close();
            }
            lock (_depotDownloaderProcessLock) {
                _depotDownloaderProcess = null;
            }
            UpdateDownpatcherButtons();
        }

        private void Hyperlink_RequestNavigate(
            object sender, RequestNavigateEventArgs e) {
            // Open up the hyperlink in a browser.
            ProcessStartInfo processStartInfo =
                new ProcessStartInfo(e.Uri.AbsoluteUri);
            processStartInfo.UseShellExecute = true;
            Process.Start(processStartInfo);
            e.Handled = true;
        }

        private void StartDownpatcherButton_Click(object sender, RoutedEventArgs e) {
            _console.Output("Beginning to downpatch.");
            if (Directory.Exists(_doomEternalDownpatchFolder)) {
                _console.Output("Clearing out downpatch folder.");
                Directory.Delete(_doomEternalDownpatchFolder, true);
            }
            DoomVersions.DoomVersion downpatchVersion = null;
            List<DoomVersions.DoomVersion> intermediateVersions =
                new List<DoomVersions.DoomVersion>();
            foreach (var version in _availableVersions.versions) {
                // Get all versions in range (downpatchVersion, installedVersion].
                if (downpatchVersion != null) {
                    intermediateVersions.Add(version);
                }
                if (version.name.Equals(cbDownpatchVersion.SelectedItem.ToString())) {
                    downpatchVersion = version;
                }
            }

            // Create file list from all intermediate version file lists.
            HashSet<string> aggregatedFiles = new HashSet<string>();
            foreach (var version in intermediateVersions) {
                string[] files = GetFileList(version.name);
                foreach (string file in files) {
                    aggregatedFiles.Add(file);
                }
            }

            // Write aggregated file list to output filelist.txt.
            string fileListPath = Directory.GetCurrentDirectory() + @"\filelist.txt";
            StreamWriter streamWriter = new StreamWriter(fileListPath, false);
            foreach (string file in aggregatedFiles) {
                streamWriter.WriteLine(file);
            }
            streamWriter.Flush();
            streamWriter.Close();

            _console.Output("Generated filelist.txt.");

            // Using the downpatchVersion manifestIds and the generated filelist.txt,
            // we now need to call DepotDownloader.
            string username = tbUsername.Text;
            string password = pbPassword.Password;

            string[] depotIds = {
                "782332", "782333", "782334", "782335", "782336", "782337", "782338",
                "782339"
            };

            // Run DepotDownloader on a new thread to not block the UI-thread. 
            new Thread(() => {
                ExecuteDepotDownloads(
                    depotIds,
                    downpatchVersion.manifestIds,
                    fileListPath,
                    username,
                    password);
            }).Start();
        }

        private void SelectDoomFolderButton_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog selectFolderDialog = new FolderBrowserDialog();
            selectFolderDialog.SelectedPath = Directory.GetCurrentDirectory();
            if (selectFolderDialog.ShowDialog()
                    == System.Windows.Forms.DialogResult.OK) {
                _doomEternalPath = 
                    ValidateDoomRootPath(selectFolderDialog.SelectedPath);
                _doomEternalDetectedVersion = InitializeDoomVersion();
                InitializeDoomDownpatchVersions();
            }
            UpdateDownpatcherButtons();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog selectFolderDialog = new FolderBrowserDialog();
            selectFolderDialog.SelectedPath = Directory.GetCurrentDirectory();
            if (selectFolderDialog.ShowDialog()
                    == System.Windows.Forms.DialogResult.OK) {
                _doomEternalDownpatchFolder = selectFolderDialog.SelectedPath;
                lSelectedFolder.Content = _doomEternalDownpatchFolder;
            }
            UpdateDownpatcherButtons();
        }

        private void DownpatchVersionComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e) {
            UpdateDownpatcherButtons();
            _console.Output(
                "Downpatch version set to " 
                + cbDownpatchVersion.SelectedItem.ToString());
        }

        private void UsernameTextBox_TextChanged(
            object sender, TextChangedEventArgs e) {
            UpdateDownpatcherButtons();
        }

        private void PasswordPasswordBox_TextChanged(
            object sender, RoutedEventArgs e) {
            UpdateDownpatcherButtons();
        }

        private void Window_Closing(
            object sender, CancelEventArgs e) {
            KillDepotDownloaderProcess();
        }

        private void CancelDownpatcherButton_Click(
            object sender, RoutedEventArgs e) {
            _console.Output("Canceling DepotDownloader.");
            KillDepotDownloaderProcess();
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.Clipboard.SetText(_console.GetDebugString());
        }

        private void Info_Click(object sender, RoutedEventArgs e) {
            InfoWindow info = new InfoWindow();
            info.Show();
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.AutomaticallyReportExceptions =
                cbReportErrors.IsChecked == true;
            Properties.Settings.Default.Save();
        }
    }
}
