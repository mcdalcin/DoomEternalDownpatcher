namespace Downpatcher {
    public partial class MainWindow {
        private class DoomVersions {
            public class DoomVersion {
                public string name;
                public long size;
                public string[] manifestIds;
            }

            public DoomVersion[] versions;
        }
    }
}
