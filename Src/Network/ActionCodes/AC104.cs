using System;
using Game;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 104 (0x68) — Lottery UI Interaction
    /// Sub 1: Client opens/clicks the lottery interface
    ///   Recv: [104][1]
    ///   Send: [104][1][status:u8]  status 1=lottery available
    /// </summary>
    public class AC104 : AC
    {
        public override int ID { get { return 104; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_LotteryUI(c, p); break;
                default: DebugSystem.Write("AC 104," + p.B + " has not been coded"); break;
            }
        }

        void Recv_LotteryUI(Player r, RecievePacket p)
        {
            DebugSystem.Write(string.Format("[AC104] Lottery UI request from player {0}", r.CharID));
            // No response needed — client handles this locally.
            // Sending a response here breaks the client lottery UI.
        }
    }
}
