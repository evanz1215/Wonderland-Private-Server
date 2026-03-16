using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Battle;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 50 — Battle Action Commands
    /// Sub 1: Receive attack/skill command from player
    /// Packet format: [50][1][src_x:byte][src_y:byte][dst_x:byte][dst_y:byte][skill_id:ushort][unk1:byte][unk2:byte]
    /// </summary>
    public class AC50_Battle : AC
    {
        public override int ID { get { return 50; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_BattleAction(c, p); break;
                default: DebugSystem.Write("AC 50," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Receive a battle action (attack/skill/flee/defend) from a player
        /// </summary>
        void Recv_BattleAction(Player r, RecievePacket p)
        {
            if (r.MyBattle == null) return;
            if (r.MyBattle.RoundState != eBattleRoundState.PrepState) return;

            p.SetPtr(6); // Skip past AC header (50, 1) + size bytes

            byte srcX = p.Unpack8();
            byte srcY = p.Unpack8();
            byte dstX = p.Unpack8();
            byte dstY = p.Unpack8();
            ushort skillID = p.Unpack16();
            byte unk1 = p.Unpack8();
            byte unk2 = p.Unpack8();

            var battle = r.MyBattle.BattleRef;
            if (battle == null) return;

            BattleAction action = new BattleAction();
            action.src = r.MyBattle.FindFighter(srcX, srcY);
            action.dst = r.MyBattle.FindFighter(dstX, dstY);
            action.unknownbyte = unk1;
            action.unknownbyte2 = unk2;

            // Resolve skill from SkillManager
            if (cGlobal.gSkillManager != null)
            {
                var skillData = cGlobal.gSkillManager.Get_Skill(skillID);
                if (skillData != null)
                {
                    var info = skillData.GetData();
                    action.skill = BattleSkill.FromSkillData(
                        info.SkillID,
                        info.EffectLayer,
                        info.AdditinalHarm,
                        info.MaxSkillLevel,
                        skillData.Grade,
                        info.SkillPattern1,
                        info.NumberOfTurns,
                        info.UnknownByte7,
                        info.SP
                    );
                }
            }

            // Fallback to basic attack if skill not found
            if (action.skill == null)
                action.skill = BattleSkill.BasicAttack();

            // Handle special actions
            if (action.skill.EffectLayer == EffectLayer.Flee)
                action.skill = BattleSkill.FleeAction();
            else if (action.skill.EffectLayer == EffectLayer.Defend)
                action.skill = BattleSkill.DefendAction();

            // Deduct SP cost
            if (action.src != null && action.skill.SPCost > 0)
            {
                if (action.src.CurSP < action.skill.SPCost)
                {
                    // Not enough SP — fallback to basic attack
                    action.skill = BattleSkill.BasicAttack();
                }
                else
                {
                    action.src.CurSP -= action.skill.SPCost;
                }
            }

            battle.PLayer_BattleAction(action);
        }
    }
}
