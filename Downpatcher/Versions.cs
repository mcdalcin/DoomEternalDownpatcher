namespace Downpatcher {
    class Versions {
        public class DoomVersion {
            public string name;
            public long size;
            public string[] manifestIds;
        }

        public string depotDownloaderVersion = "";
        public DoomVersion[] versions = new DoomVersion[0];
    }
}
