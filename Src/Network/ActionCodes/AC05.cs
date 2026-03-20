using System;
using Game;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 5 — Character State / Equipment / Warp Requests
    /// Sub 7:  Unknown (reference echoes charID + value back as AC 5,8)
    /// Sub 17: Warp/spawn request (e.g., spawn recorded spot, jail, general warp)
    ///   Recv: [5][17][warpSlot:u8]
    /// </summary>
    public class AC05 : AC
    {
        public override int ID { get { return 5; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 7: Recv_7(c, p); break;
                case 17: Recv_17(c, p); break;
                default:
                    DebugSystem.Write(string.Format("[AC05] Sub {0} unhandled from {1}", p.B, c.CharID));
                    break;
            }
        }

        void Recv_7(Player r, RecievePacket p)
        {
            // Reference: echoes back charID + value as AC 5,8
            p.SetPtr(6);
            byte value = p.Unpack8();
            SendPacket resp = new SendPacket();
            resp.PackArray(new byte[] { 5, 8 });
            resp.Pack32(r.CharID);
            resp.Pack8(value);
            r.Send(resp);
        }

        void Recv_17(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte warpSlot = p.Unpack8();
            DebugSystem.Write(string.Format("[AC05] Warp request slot={0} from {1}({2})", warpSlot, r.CharName, r.CharID));

            // Reference: requires character.state > 0 to process
            // warpSlot meanings:
            //   1 = spawn to starter beach
            //   2 = spawn recorded spot (no-op in reference)
            //   4 = out of jail
            //   5 = jail
            //   6 = general warp request (mapTo, x, y follow in packet)
            // For now, just log — most warp slots are no-ops or need more context
        }
    }
}
