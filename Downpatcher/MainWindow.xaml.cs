using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Downpatcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private readonly string _doomEternalPath = "";

        public MainWindow() {
            InitializeComponent();
            _doomEternalPath = InitializeDoomRootPath();
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

        private void InitializeDoomVersion() {
            // Get size of DoomEternal
        }
    }
}
