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
    /// AC 62 — Tent Management / Decoration
    /// Sub 1: Place item in tent
    /// Sub 3: Move/rotate item in tent
    /// Sub 4: Pick up item from tent
    /// Sub 8: Set floor appearance (color/wallpaper)
    /// </summary>
    public class AC62_Tent : AC
    {
        public override int ID { get { return 62; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_PlaceItem(c, p); break;
                case 3: Recv_MoveItem(c, p); break;
                case 4: Recv_PickupItem(c, p); break;
                case 8: Recv_SetAppearance(c, p); break;
                default: DebugSystem.Write("AC 62," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Place an inventory item into the tent.
        /// Packet: [62][1][invSlot:byte][floor:byte][x:ushort][y:ushort][z:ushort][rotation:byte]
        /// </summary>
        void Recv_PlaceItem(Player r, RecievePacket p)
        {
            if (r.Tent == null) return;

            p.SetPtr(6);
            byte invSlot = p.Unpack8();
            byte floor = p.Unpack8();
            ushort x = p.Unpack16();
            ushort y = p.Unpack16();
            ushort z = p.Unpack16();
            byte rotation = p.Unpack8();

            r.Tent.PlaceItem(r, invSlot, floor, x, y, z, rotation);
        }

        /// <summary>
        /// Move/rotate a placed item.
        /// Packet: [62][3][floor:byte][slot:byte][x:ushort][y:ushort][rotation:byte]
        /// </summary>
        void Recv_MoveItem(Player r, RecievePacket p)
        {
            if (r.Tent == null) return;

            p.SetPtr(6);
            byte floor = p.Unpack8();
            byte slot = p.Unpack8();
            ushort x = p.Unpack16();
            ushort y = p.Unpack16();
            byte rotation = p.Unpack8();

            r.Tent.MoveItem(r, floor, slot, x, y, rotation);
        }

        /// <summary>
        /// Pick up a placed item (return to inventory).
        /// Packet: [62][4][floor:byte][slot:byte]
        /// </summary>
        void Recv_PickupItem(Player r, RecievePacket p)
        {
            if (r.Tent == null) return;

            p.SetPtr(6);
            byte floor = p.Unpack8();
            byte slot = p.Unpack8();

            r.Tent.PickupItem(r, floor, slot);
        }

        /// <summary>
        /// Set floor color and wallpaper.
        /// Packet: [62][8][floor:byte][floorColor:ushort][wallpaper:ushort]
        /// </summary>
        void Recv_SetAppearance(Player r, RecievePacket p)
        {
            if (r.Tent == null) return;

            p.SetPtr(6);
            byte floor = p.Unpack8();
            ushort floorColor = p.Unpack16();
            ushort wallpaper = p.Unpack16();

            r.Tent.SetFloorAppearance(r, floor, floorColor, wallpaper);
        }
    }
}
