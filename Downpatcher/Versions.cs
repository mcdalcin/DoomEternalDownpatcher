using System;

namespace Downpatcher {
    internal class Versions {
        public class DoomVersion {
            public string name;
            public long size;
            public string[] manifestIds;
        }

        public string depotDownloaderVersion = "";
        public DoomVersion[] versions = Array.Empty<DoomVersion>();
    }
}
