using System;
using System.IO;
using Game;
using Plugin;

namespace wlo.pserver.maps
{
    public class Map10019_Lobby : GameMap
    {
        public Map10019_Lobby() : base() { m_mapid = 10019; }
        public Map10019_Lobby(PluginHost host, FileInfo src) : base(host, src) { m_mapid = 10019; }

        public override uint MapID { get { return 10019; } set { } }
        public override string MapName { get { return "Lobby"; } }

        protected override void LoadData()
        {
            DebugSystem.Write("[" + System.Reflection.Assembly.GetAssembly(this.GetType()).FullName + "] - Initializing Map " + MapID + " - " + MapName);
        }
    }
}
