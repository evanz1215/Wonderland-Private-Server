using System;
using Game;
using Network;
using Server;
using Server.System;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 34 — Online Lottery (線上抽獎)
    /// Sub 1: Client sends draw request
    ///   Recv: [34][1][type:u8]  type 0=gold lottery, 1=IM lottery
    ///   Send: [34][1][type:u8][result:u8][itemID:u16][amount:u8]
    ///         result: 1=success, 0=fail (insufficient funds), 2=no prizes available
    /// </summary>
    public class AC34_Lottery : AC
    {
        public override int ID { get { return 34; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_Draw(c, p); break;
                default: DebugSystem.Write("AC 34," + p.B + " has not been coded"); break;
            }
        }

        void Recv_Draw(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte type = p.Unpack8(); // 0=gold, 1=IM

            DebugSystem.Write(string.Format("[AC34] Lottery draw from player {0}, type={1}",
                r.CharID, type == 0 ? "Gold" : "IM"));

            var lottery = cGlobal.gLotteryManager;
            if (lottery == null)
            {
                DebugSystem.Write("[AC34] LotteryManager not initialized");
                SendResult(r, type, 2, 0, 0); // no prizes
                return;
            }

            // Check cost
            if (type == 0)
            {
                // Gold lottery
                if (r.Gold < (uint)lottery.GoldCost)
                {
                    DebugSystem.Write(string.Format("[AC34] Player {0} insufficient gold: has {1}, needs {2}",
                        r.CharID, r.Gold, lottery.GoldCost));
                    SendResult(r, type, 0, 0, 0);
                    return;
                }
            }
            else
            {
                // IM lottery
                if (r.UserAcc == null || r.UserAcc.IM < lottery.ImCost)
                {
                    int has = r.UserAcc != null ? r.UserAcc.IM : 0;
                    DebugSystem.Write(string.Format("[AC34] Player {0} insufficient IM: has {1}, needs {2}",
                        r.CharID, has, lottery.ImCost));
                    SendResult(r, type, 0, 0, 0);
                    return;
                }
            }

            // Draw prize
            var prize = lottery.Draw(type);
            if (prize == null)
            {
                DebugSystem.Write("[AC34] No prizes available for type " + type);
                SendResult(r, type, 2, 0, 0);
                return;
            }

            // Deduct cost
            if (type == 0)
            {
                r.AddGold(-lottery.GoldCost);
                r.SendGold();
            }
            else
            {
                r.UserAcc.IM -= lottery.ImCost;
                if (r.UserAcc.IM < 0) r.UserAcc.IM = 0;
                cGlobal.gUserDataBase.UpdateUser(r.UserAcc.DataBaseID, im: r.UserAcc.IM);
                // Send updated IM balance
                r.Send(Tools.FromFormat("bbdddd", 35, 4, r.UserAcc.IM, 0, 0, 0));
            }

            // Give item
            r.Inv.AddItem(prize.ItemID, prize.Amount);
            GameLogger.LogItemGain(r, prize.ItemID, prize.Amount, type == 0 ? "GoldLottery" : "IMLottery");

            // Send result
            SendResult(r, type, 1, prize.ItemID, prize.Amount);

            DebugSystem.Write(string.Format("[AC34] Player {0} won item {1} ({2}) x{3}",
                r.CharID, prize.ItemID, prize.Name, prize.Amount));
        }

        /// <summary>
        /// Send lottery result to client.
        /// Format: [34][1][type][result][itemID:u16][amount]
        /// </summary>
        void SendResult(Player r, byte type, byte result, ushort itemID, byte amount)
        {
            r.Send(Tools.FromFormat("bbbbbWb", 34, 1, type, result, 0, itemID, amount));
        }
    }
}
