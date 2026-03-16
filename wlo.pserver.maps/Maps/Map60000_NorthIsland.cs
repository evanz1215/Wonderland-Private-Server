using System;
using System.IO;
using Game;
using Game.Maps;
using Plugin;

namespace wlo.pserver.maps
{
    public class Map60000_NorthIsland : GameMap
    {
        public Map60000_NorthIsland() : base() { m_mapid = 60000; }
        public Map60000_NorthIsland(PluginHost host, FileInfo src) : base(host, src) { m_mapid = 60000; }

        public override uint MapID { get { return 60000; } set { } }
        public override string MapName { get { return "North Island"; } }

        protected override void LoadData()
        {
            DebugSystem.Write("[" + System.Reflection.Assembly.GetAssembly(this.GetType()).FullName + "] - Initializing Map " + MapID + " - " + MapName);

            // Portal 1: North Island → South Bay (11016)
            var dest = new WarpDest { clickID = 1, DstID = 11016, DstX = 1181, DstY = 243 };
            Destinations[1] = dest;

            var portal = new WarpPortal { DstID = 1 };
            Portals[1] = portal;
        }
    }
}
