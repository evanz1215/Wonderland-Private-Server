using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Network;
using RCLibrary.Core.Networking;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 08 — Character Stats
    /// Sub 1: Allocate potential points into base stats
    /// </summary>
    public class AC08 : AC
    {
        public override int ID { get { return 8; } }

        public override void ProcessPkt(Player r, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_AllocateStats(r, p); break;
                default: DebugSystem.Write("AC 8," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Allocate potential points into base stats.
        /// Packet: [8][1][count:8][statID:8][amount:32] × count
        /// Stat IDs: 28=Str, 29=Con, 30=Agi, 27=Int, 33=Wis
        /// </summary>
        void Recv_AllocateStats(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                int count = p.Unpack8();

                if (count <= 0 || count > 5)
                    return;

                // Track total points requested to validate against available potential
                int totalRequested = 0;
                var allocations = new List<KeyValuePair<byte, byte>>();

                for (int i = 0; i < count; i++)
                {
                    byte statID = (byte)p.Unpack8();
                    byte amount = (byte)p.Unpack32();

                    if (amount == 0) continue;

                    // Validate stat ID
                    if (statID != 28 && statID != 29 && statID != 30 && statID != 27 && statID != 33)
                        continue;

                    totalRequested += amount;
                    allocations.Add(new KeyValuePair<byte, byte>(statID, amount));
                }

                if (totalRequested == 0 || totalRequested > r.Potential)
                    return;

                // Apply all allocations
                foreach (var alloc in allocations)
                {
                    r.AllocateStat(alloc.Key, alloc.Value);
                }

                // Send updated stats to client
                r.Send8_1(false);

                // Send updated potential
                SendPacket pkt = new SendPacket();
                pkt.PackArray(Tools.FromFormatToArray("bbbbdd", 8, 1, 34, 1, r.Potential, 0));
                r.Send(pkt);
            }
            catch (Exception ex) { log.Error(ex.Message, ex); }
        }
    }
}
