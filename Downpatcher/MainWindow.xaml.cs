using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const string DOOM_ETERNAL_EXE_STRING = "DOOMEternalx64vk.exe";
        private const string DOOM_ETERNAL_VERSION_URL = "https://raw.githubusercontent.com/mcdalcin/DoomEternalDownpatcher/master/data/versions.json";
        private const string DEPOT_DOWNLOADER_LATEST_URL = "https://api.github.com/repos/SteamRE/DepotDownloader/releases/latest";

        private readonly string _doomEternalPath = "";
        private readonly string _doomEternalDetectedVersion = "";

        private string _doomEternalDownpatchFolder = "";
        private string _depotDownloaderInstallPath = "";

        private static Versions _availableVersions;

        private ConsoleContent console;

        private class Versions {
            public class Version {
                public string name;
                public long size;
            }

            public Version[] versions;
        }

        public MainWindow() {
            InitializeComponent();
            console = new ConsoleContent(scroller);
            DataContext = console;
            _doomEternalPath = InitializeDoomRootPath();
            _doomEternalDetectedVersion = InitializeDoomVersion();
            InitializeDoomDownpatchVersions();
            InitializeDoomDownpatchFolder();
            InitializeDepotDownloader();
        }

        private string InitializeDoomRootPath() {
            // Get steam base path from registry.
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam");
            if (localKey == null) {
                return "";
            }
        
            string steamPath = localKey.GetValue("InstallPath").ToString();
            string doomEternalPath = steamPath + @"\steamapps\common\DOOMEternal";

            // Check that the DOOM Eternal folder exists.
            if (Directory.Exists(doomEternalPath)) {
                console.Output("Successfully found DOOM Eternal installation folder!");
                tbRootPath.Inlines.Add(new Bold(new Run("Root folder found: ")) { 
                    Foreground = Brushes.LimeGreen
                });
                tbRootPath.Inlines.Add(new Run(doomEternalPath));
                return doomEternalPath;
            } else {
                console.Output("ERROR: Could not find DOOM Eternal installation folder!");
                tbRootPath.Inlines.Add(new Bold(new Run("Unable to find DOOM Eternal root folder.")) {
                    Foreground = Brushes.Red
                });
                return "";
            }
        }

        private string InitializeDoomVersion() {
            // Get size of Doom Eternal exe.
            string doomEternalExePath = _doomEternalPath + @"\" + DOOM_ETERNAL_EXE_STRING;

            if (!File.Exists(doomEternalExePath)) {
                return "";
            }

            long exeSize = new FileInfo(doomEternalExePath).Length;
            string doomVersion = DetermineDoomVersion(exeSize);

            if (doomVersion.Length != 0) {
                console.Output("Successfully detected installed DOOM Eternal version: " + doomVersion);
                tbVersion.Inlines.Add(new Bold(new Run("Installed DOOM Eternal version: ") {
                    Foreground = Brushes.LimeGreen
                }));
                tbVersion.Inlines.Add(new Run(doomVersion));
            } else {
                console.Output("ERROR: Unable to detect installed DOOM Eternal version.");
                tbVersion.Inlines.Add(new Bold(new Run("Unable to determine DOOM Eternal version.") {
                    Foreground = Brushes.Red
                }));
                return "";
            }
            return doomVersion;
        }

        private void InitializeDepotDownloader() {
            // Check for latest depot downloader release located in current working directory.
            using (var webClient = new WebClient()) {
                webClient.Headers.Add("User-Agent: Other");
                dynamic json = JsonConvert.DeserializeObject(webClient.DownloadString(DEPOT_DOWNLOADER_LATEST_URL));
                string name = json.name;
                string fileName = json.assets[0].name;
                string downloadUrl = json.assets[0].browser_download_url;
                _depotDownloaderInstallPath = Directory.GetCurrentDirectory() + @"\" + Path.GetFileNameWithoutExtension(fileName);
                // If latest DepotDownloader is not already installed, download and unzip it.
                if (!Directory.Exists(_depotDownloaderInstallPath)) {
                    console.Output("New DepotDownloader version detected. Installing " + name + ".");
                    console.Output("Downloading " + downloadUrl);
                    webClient.DownloadFile(downloadUrl, Directory.GetCurrentDirectory() + @"\" + fileName);
                    console.Output("Unpacking downloaded files.");
                    ZipFile.ExtractToDirectory(fileName, _depotDownloaderInstallPath);
                    console.Output("Successfully unpacked DepotDownloader!");
                }
                console.Output(name + " installed.");
            }
        }

        private void InitializeDoomDownpatchVersions() {
            // For now, only include versions less than our current version.
            int count = 0;
            foreach(var version in _availableVersions.versions) {
                if (_doomEternalDetectedVersion.Equals(version.name)) {
                    break;
                }
                cbDownpatchVersion.Items.Add(version.name);
                count++;
            }
            console.Output("There are " + count + " available downpatch versions. Please pick one above.");
        }

        private void InitializeDoomDownpatchFolder() {
            // For now, let's use the current folder + DOWNPATCH_FILES.
            _doomEternalDownpatchFolder = Directory.GetCurrentDirectory() + @"\DOWNPATCH_FILES";
            lSelectedFolder.Content = new Run(_doomEternalDownpatchFolder);
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog selectFolderDialog = new CommonOpenFileDialog() {
                IsFolderPicker = true
            };
            if (selectFolderDialog.ShowDialog() == CommonFileDialogResult.Ok) {
                _doomEternalDownpatchFolder = selectFolderDialog.FileName;
                lSelectedFolder.Content = _doomEternalDownpatchFolder;
            }
        }

        private void UpdateStartDownpatcherButton_IsEnabled() {
            bStartDownpatcher.IsEnabled =
                cbDownpatchVersion.SelectedItem.ToString().Length != 0 &&
                _doomEternalDetectedVersion.Length != 0 &&
                _doomEternalDownpatchFolder.Length != 0 &&
                _depotDownloaderInstallPath.Length != 0;
        }

        private void StartDownpatcherButton_Click(object sender, RoutedEventArgs e) {
            // Check to make sure a downpatch version and downpatch folder are selected and valid.

        }

        private void DownpatchVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateStartDownpatcherButton_IsEnabled();
            console.Output("Downpatch version set to " + cbDownpatchVersion.SelectedItem.ToString());
        }

        private static string DetermineDoomVersion(long exeSize) {
            using (var webClient = new WebClient()) {
                var json = webClient.DownloadString(DOOM_ETERNAL_VERSION_URL);
                _availableVersions = JsonConvert.DeserializeObject<Versions>(json);
                foreach (var version in _availableVersions.versions) {
                    if (version.size == exeSize) {
                        return version.name;
                    }
                }
            }

            return "";
        }
    }
}
