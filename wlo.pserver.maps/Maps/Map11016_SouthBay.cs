using System;
using System.IO;
using Game;
using Game.Maps;
using Plugin;

namespace wlo.pserver.maps
{
    public class Map11016_SouthBay : GameMap
    {
        public Map11016_SouthBay() : base() { m_mapid = 11016; }
        public Map11016_SouthBay(PluginHost host, FileInfo src) : base(host, src) { m_mapid = 11016; }

        public override uint MapID { get { return 11016; } set { } }
        public override string MapName { get { return "South Bay"; } }

        protected override void LoadData()
        {
            DebugSystem.Write("[" + System.Reflection.Assembly.GetAssembly(this.GetType()).FullName + "] - Initializing Map " + MapID + " - " + MapName);

            // Portal 1: South Bay → North Island (60000)
            var dest = new WarpDest { clickID = 1, DstID = 60000, DstX = 500, DstY = 400 };
            Destinations[1] = dest;

            var portal = new WarpPortal { DstID = 1 };
            Portals[1] = portal;
        }
    }
}
