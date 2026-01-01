using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CMyServer
    {
        // MyServer
        public bool MyServerEnabled { get; set; } = false;
        public string MyServerUrl { get; set; } = "";
        public string MyServerKey { get; set; } = "";
        public string MyAddSongInfoPage { get; set; } = "";
        public string MyAddSongInfoNumberVar { get; set; } = "";
        public string MyAddSongInfoTitleVar { get; set; } = "";
        public string MyAddSongInfoArtistVar { get; set; } = "";
        public string MyAddSongInfoLinkVar { get; set; } = "";
        public string MyAddSongInfoLinkFolderOnServer { get; set; } = "";
        public string MyRemoveFilePrefix { get; set; } = "";
    }
}
