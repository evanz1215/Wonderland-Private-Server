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
    /// AC 29 — Equipment Enhancement System
    /// Sub 1: Forge (鍛造) — increase forge level
    /// Sub 2: Socket (嵌寶) — insert gem into equipment
    /// Sub 3: Unsocket (拆寶) — remove gem from equipment
    /// Sub 4: Bomb (轟炸) — add bomb effect
    /// Sub 5: Sew (縫紉) — add sew effect
    /// </summary>
    public class AC29_Enhance : AC
    {
        public override int ID { get { return 29; } }

        static readonly Random rng = new Random();

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_Forge(c, p); break;
                case 2: Recv_Socket(c, p); break;
                case 3: Recv_Unsocket(c, p); break;
                case 4: Recv_Bomb(c, p); break;
                case 5: Recv_Sew(c, p); break;
                default: DebugSystem.Write("AC 29," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Forge equipment: increase forge level by 1.
        /// Packet: [29][1][equipSlot:byte]
        /// Success rate decreases with higher forge levels.
        /// On failure at +5 or above, forge level resets to 0.
        /// </summary>
        void Recv_Forge(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte equipSlot = p.Unpack8();

            if (equipSlot < 1 || equipSlot > 6) return;

            var equip = r.GetEquip(equipSlot);
            if (equip == null || equip.ItemID == 0) return;

            byte currentForge = equip.Forge;
            if (currentForge >= 10)
            {
                SendResult(r, (byte)EnhanceType.Forge, 2); // already max
                return;
            }

            // Cost: 1000 * (currentForge + 1) gold
            int cost = 1000 * (currentForge + 1);
            if (r.Gold < cost)
            {
                SendResult(r, (byte)EnhanceType.Forge, 3); // not enough gold
                return;
            }

            r.TakeGold(cost);

            // Success rate: 100% for +0→+1, decreasing 10% each level
            int successRate = 100 - (currentForge * 10);
            if (successRate < 10) successRate = 10;

            if (rng.Next(100) < successRate)
            {
                equip.Forge = (byte)(currentForge + 1);
                SendResult(r, (byte)EnhanceType.Forge, 0, equipSlot, equip.Forge); // success
                r.Send8_1(false);
            }
            else
            {
                // Failure: if forge >= 5, reset to 0
                if (currentForge >= 5)
                    equip.Forge = 0;
                SendResult(r, (byte)EnhanceType.Forge, 1, equipSlot, equip.Forge); // failure
                r.Send8_1(false);
            }
        }

        /// <summary>
        /// Socket a gem into equipment.
        /// Packet: [29][2][equipSlot:byte][gemInvSlot:byte]
        /// </summary>
        void Recv_Socket(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte equipSlot = p.Unpack8();
            byte gemSlot = p.Unpack8();

            if (equipSlot < 1 || equipSlot > 6) return;
            if (gemSlot < 1 || gemSlot > 50) return;

            var equip = r.GetEquip(equipSlot);
            if (equip == null || equip.ItemID == 0) return;

            var gem = r.Inv[gemSlot];
            if (gem == null || gem.ItemID == 0) return;

            if (equip.SocketID != 0)
            {
                SendResult(r, (byte)EnhanceType.Socket, 2); // slot already occupied
                return;
            }

            equip.SocketID = gem.ItemID;
            r.Inv.RemoveItem(gemSlot, 1);
            SendResult(r, (byte)EnhanceType.Socket, 0, equipSlot, (byte)equip.SocketID);
            r.Send8_1(false);
        }

        /// <summary>
        /// Remove gem from equipment. Gem is returned to inventory.
        /// Packet: [29][3][equipSlot:byte]
        /// </summary>
        void Recv_Unsocket(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte equipSlot = p.Unpack8();

            if (equipSlot < 1 || equipSlot > 6) return;

            var equip = r.GetEquip(equipSlot);
            if (equip == null || equip.ItemID == 0) return;

            if (equip.SocketID == 0)
            {
                SendResult(r, (byte)EnhanceType.Unsocket, 2); // no gem
                return;
            }

            // Cost to unsocket: 500 gold
            if (r.Gold < 500)
            {
                SendResult(r, (byte)EnhanceType.Unsocket, 3); // not enough gold
                return;
            }

            ushort gemID = (ushort)equip.SocketID;
            r.TakeGold(500);
            equip.SocketID = 0;

            r.Inv.AddItem(gemID, 1);
            SendResult(r, (byte)EnhanceType.Unsocket, 0, equipSlot, 0);
            r.Send8_1(false);
        }

        /// <summary>
        /// Apply bomb effect to equipment.
        /// Packet: [29][4][equipSlot:byte][bombInvSlot:byte]
        /// </summary>
        void Recv_Bomb(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte equipSlot = p.Unpack8();
            byte bombSlot = p.Unpack8();

            if (equipSlot < 1 || equipSlot > 6) return;
            if (bombSlot < 1 || bombSlot > 50) return;

            var equip = r.GetEquip(equipSlot);
            if (equip == null || equip.ItemID == 0) return;

            var bomb = r.Inv[bombSlot];
            if (bomb == null || bomb.ItemID == 0) return;

            equip.BombID = bomb.ItemID;
            r.Inv.RemoveItem(bombSlot, 1);
            SendResult(r, (byte)EnhanceType.Bomb, 0, equipSlot, (byte)equip.BombID);
            r.Send8_1(false);
        }

        /// <summary>
        /// Apply sew effect to equipment.
        /// Packet: [29][5][equipSlot:byte][sewInvSlot:byte]
        /// </summary>
        void Recv_Sew(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte equipSlot = p.Unpack8();
            byte sewSlot = p.Unpack8();

            if (equipSlot < 1 || equipSlot > 6) return;
            if (sewSlot < 1 || sewSlot > 50) return;

            var equip = r.GetEquip(equipSlot);
            if (equip == null || equip.ItemID == 0) return;

            var sew = r.Inv[sewSlot];
            if (sew == null || sew.ItemID == 0) return;

            equip.SewID = sew.ItemID;
            r.Inv.RemoveItem(sewSlot, 1);
            SendResult(r, (byte)EnhanceType.Sew, 0, equipSlot, (byte)equip.SewID);
            r.Send8_1(false);
        }

        /// <summary>
        /// Send enhancement result to client.
        /// Packet: [29][10][enhanceType:byte][resultCode:byte][slot:byte][newValue:byte]
        /// resultCode: 0=success, 1=failure, 2=already occupied/max, 3=not enough gold
        /// </summary>
        void SendResult(Player r, byte enhanceType, byte resultCode, byte slot = 0, byte newValue = 0)
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(29);
            pkt.Pack8(10);
            pkt.Pack8(enhanceType);
            pkt.Pack8(resultCode);
            pkt.Pack8(slot);
            pkt.Pack8(newValue);
            r.Send(pkt);
        }
    }
}
