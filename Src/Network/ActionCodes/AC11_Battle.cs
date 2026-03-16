using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Battle;
using Game.Maps;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 11 — Battle Initiation
    /// Sub 1: Battle leave/flee
    /// Sub 2: PK / NPC battle initiation
    /// </summary>
    public class AC11_Battle : AC
    {
        public override int ID { get { return 11; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_1(c, p); break;
                case 2: Recv_2(c, p); break;
                default: DebugSystem.Write("AC 11," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Battle leave/flee request
        /// </summary>
        void Recv_1(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte subtype = p.Unpack8();
            switch (subtype)
            {
                case 3: // Run away
                    if (r.MyBattle != null)
                        r.MyBattle.BattleRef.RemFighter(eBattleLeaveType.RunAway, r);
                    break;
            }
        }

        /// <summary>
        /// PK / NPC battle initiation
        /// </summary>
        void Recv_2(Player r, RecievePacket p)
        {
            if (r.MyBattle != null) return; // already in battle
            if (r.CurMap == null) return;

            p.SetPtr(6);
            byte pkType = p.Unpack8();
            uint targetID = p.Unpack32();
            ushort clickID = p.Unpack16();

            switch (pkType)
            {
                case 3: // NPC battle
                    {
                        if (cGlobal.gNpcManager == null) return;
                        var npcData = cGlobal.gNpcManager.GetNpcbyID((ushort)targetID);
                        if (npcData == null)
                        {
                            DebugSystem.Write(DebugItemType.Info_Heavy, "NPC {0} not found in Npc.dat", targetID);
                            return;
                        }

                        string name = "";
                        if (npcData.NpcName != null)
                            name = System.Text.ASCIIEncoding.ASCII.GetString(npcData.NpcName).TrimEnd('\0');

                        var mob = new MobFighter(
                            npcData.NpcID, name, npcData.Level,
                            npcData.HP, npcData.SP,
                            npcData.STR, npcData.CON, npcData.INT, npcData.WIS, npcData.AGI, npcData.SPD,
                            npcData.element,
                            new ushort[] { npcData.SkillID1, npcData.SkillID2, npcData.SkillID3 },
                            new ushort[] { npcData.ItemID1, npcData.ItemID2, npcData.ItemID3, npcData.ItemID4, npcData.ItemID5 },
                            npcData.Catchable == 1
                        );
                        mob.ClickID = clickID;

                        if (r.CurMap is GameMap)
                            ((GameMap)r.CurMap).onNpcPk(r, mob);
                    }
                    break;
            }
        }
    }
}
