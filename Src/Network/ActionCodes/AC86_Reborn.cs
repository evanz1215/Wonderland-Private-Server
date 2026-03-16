using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Maps;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 86 — Reborn / Rebirth System
    /// Sub 1: Request reborn (check eligibility)
    /// Sub 2: Confirm reborn with job selection
    /// </summary>
    public class AC86_Reborn : AC
    {
        public override int ID { get { return 86; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_RequestReborn(c, p); break;
                case 2: Recv_ConfirmReborn(c, p); break;
                default: DebugSystem.Write("AC 86," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Client requests reborn eligibility check.
        /// Packet: [86][1]
        /// Response: [86][1][result:byte]
        ///   result: 0 = eligible, 1 = already reborn, 2 = level too low
        /// </summary>
        void Recv_RequestReborn(Player r, RecievePacket p)
        {
            SendPacket s = new SendPacket();
            s.Pack8(86);
            s.Pack8(1);

            if (r.Reborn)
            {
                s.Pack8(1); // already reborn
            }
            else if (r.Level < 199)
            {
                s.Pack8(2); // level too low
            }
            else
            {
                s.Pack8(0); // eligible — show job selection UI

                // Send available jobs
                s.Pack8(7); // number of jobs
                s.Pack8((byte)RebornJob.Killer);
                s.PackStringN("Killer");
                s.Pack8((byte)RebornJob.Warrior);
                s.PackStringN("Warrior");
                s.Pack8((byte)RebornJob.Knight);
                s.PackStringN("Knight");
                s.Pack8((byte)RebornJob.Wit);
                s.PackStringN("Wit");
                s.Pack8((byte)RebornJob.Priest);
                s.PackStringN("Priest");
                s.Pack8((byte)RebornJob.Seer);
                s.PackStringN("Seer");
                s.Pack8((byte)RebornJob.NissRB);
                s.PackStringN("NissRB");
            }
            r.Send(s);
        }

        /// <summary>
        /// Client confirms reborn with selected job.
        /// Packet: [86][2][jobID:byte]
        /// Response: [86][2][result:byte]
        ///   result: 0 = success, 1 = failed
        /// </summary>
        void Recv_ConfirmReborn(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte jobID = p.Unpack8();

            RebornJob selectedJob = (RebornJob)jobID;

            // Validate job selection
            if (selectedJob == RebornJob.none || (byte)selectedJob > 7)
            {
                SendResult(r, 1); // invalid job
                return;
            }

            if (!r.CanReborn())
            {
                SendResult(r, 1); // can't reborn
                return;
            }

            if (r.PerformReborn(selectedJob))
            {
                SendResult(r, 0); // success

                // Broadcast reborn to map (other players see the change)
                if (r.CurMap is GameMap)
                {
                    SendPacket notify = new SendPacket();
                    notify.Pack8(86);
                    notify.Pack8(3);
                    notify.Pack32(r.CharID);
                    notify.Pack8((byte)selectedJob);
                    ((GameMap)r.CurMap).Broadcast(notify, "Ex", r.CharID);
                }
            }
            else
            {
                SendResult(r, 1); // failed
            }
        }

        void SendResult(Player r, byte result)
        {
            SendPacket s = new SendPacket();
            s.Pack8(86);
            s.Pack8(2);
            s.Pack8(result);
            r.Send(s);
        }
    }
}
