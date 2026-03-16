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
    /// AC 15 — Pet System
    /// Sub 3: Dismiss/release pet
    /// Sub 5: Bring pet into battle
    /// Sub 6: Rest pet (remove from battle)
    /// Sub 16: Ride pet
    /// Sub 17: Unride pet
    /// </summary>
    public class AC15_Pet : AC
    {
        public override int ID { get { return 15; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 3: Recv_DismissPet(c, p); break;
                case 5: Recv_BringIntoBattle(c, p); break;
                case 6: Recv_RestPet(c, p); break;
                case 16: Recv_RidePet(c, p); break;
                case 17: Recv_UnridePet(c, p); break;
                default: DebugSystem.Write("AC 15," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Dismiss/release a pet
        /// Packet: [15][3][slot:byte]
        /// </summary>
        void Recv_DismissPet(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte slot = p.Unpack8();
            r.Pets.DismissPet(slot);
        }

        /// <summary>
        /// Bring pet into battle mode
        /// Packet: [15][5][slot:byte]
        /// </summary>
        void Recv_BringIntoBattle(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte slot = p.Unpack8();
            r.Pets.BringIntoBattle(slot);
        }

        /// <summary>
        /// Rest the current battle pet
        /// Packet: [15][6]
        /// </summary>
        void Recv_RestPet(Player r, RecievePacket p)
        {
            r.Pets.RestPet();
        }

        /// <summary>
        /// Ride a pet
        /// Packet: [15][16][slot:byte]
        /// </summary>
        void Recv_RidePet(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte slot = p.Unpack8();
            r.Pets.RidePetAction(slot);
        }

        /// <summary>
        /// Unride the current pet
        /// Packet: [15][17]
        /// </summary>
        void Recv_UnridePet(Player r, RecievePacket p)
        {
            r.Pets.UnridePet();
        }
    }
}
