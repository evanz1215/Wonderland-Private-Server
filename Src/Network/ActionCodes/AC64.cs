using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 64 — Tent Building / Construction
    /// Sub 1: Start building (create new object in tent)
    /// Sub 2: Continue building
    /// Sub 3: Stop building / cancel
    /// </summary>
    public class AC64_TentBuild : AC
    {
        public override int ID { get { return 64; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_StartBuild(c, p); break;
                case 2: Recv_ContinueBuild(c, p); break;
                case 3: Recv_StopBuild(c, p); break;
                default: DebugSystem.Write("AC 64," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Start building a new tent item.
        /// Packet: [64][1][invSlot:byte][floor:byte][x:ushort][y:ushort][z:ushort][rotation:byte]
        /// </summary>
        void Recv_StartBuild(Player r, RecievePacket p)
        {
            if (r.Tent == null) return;

            p.SetPtr(6);
            byte invSlot = p.Unpack8();
            byte floor = p.Unpack8();
            ushort x = p.Unpack16();
            ushort y = p.Unpack16();
            ushort z = p.Unpack16();
            byte rotation = p.Unpack8();

            // For now, directly place the item (building timer not yet implemented)
            r.Tent.PlaceItem(r, invSlot, floor, x, y, z, rotation);
        }

        /// <summary>
        /// Continue building (currently no-op — building is instant).
        /// Packet: [64][2][slot:byte]
        /// </summary>
        void Recv_ContinueBuild(Player r, RecievePacket p)
        {
            // Building timer not yet implemented — building is instant
        }

        /// <summary>
        /// Stop/cancel building.
        /// Packet: [64][3][slot:byte]
        /// </summary>
        void Recv_StopBuild(Player r, RecievePacket p)
        {
            // Building timer not yet implemented — building is instant
        }
    }
}
