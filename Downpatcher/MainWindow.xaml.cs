using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const string DOOM_ETERNAL_EXE_STRING = "DOOMEternalx64vk.exe";
        private const string DOOM_ETERNAL_VERSION_URL = "https://raw.githubusercontent.com/mcdalcin/DoomEternalDownpatcher/master/data/versions.json";

        private readonly string _doomEternalPath = "";
        private readonly string _doomEternalDetectedVersion = "";

        private string _doomEternalDownpatchFolder = "";

        private static Versions _availableVersions;

        private class Versions {
            public class Version {
                public string name;
                public long size;
            }

            public Version[] versions;
        }

        public MainWindow() {
            InitializeComponent();
            _doomEternalPath = InitializeDoomRootPath();
            _doomEternalDetectedVersion = InitializeDoomVersion();
            InitializeDoomDownpatchVersions();
            InitializeDoomDownpatchFolder();
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
                tbRootPath.Inlines.Add(new Bold(new Run("Root folder found: ")) { 
                    Foreground = Brushes.LimeGreen
                });
                tbRootPath.Inlines.Add(new Run(doomEternalPath));
                return doomEternalPath;
            } else {
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
                tbVersion.Inlines.Add(new Bold(new Run("Installed DOOM Eternal version: ") {
                    Foreground = Brushes.LimeGreen
                }));
                tbVersion.Inlines.Add(new Run(doomVersion));
            } else {
                tbVersion.Inlines.Add(new Bold(new Run("Unable to determine DOOM Eternal version.") {
                    Foreground = Brushes.Red
                }));
                return "";
            }
            return doomVersion;
        }

        private void InitializeDoomDownpatchVersions() {
            // For now, only include versions less than our current version.
            foreach(var version in _availableVersions.versions) {
                if (_doomEternalDetectedVersion.Equals(version.name)) {
                    break;
                }
                cbDownpatchVersion.Items.Add(version.name);
            }
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

        private static string DetermineDoomVersion(long exeSize) {
            using (var webClient = new System.Net.WebClient()) {
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

        private void Button_Click(object sender, RoutedEventArgs e) {

        }
    }
}
