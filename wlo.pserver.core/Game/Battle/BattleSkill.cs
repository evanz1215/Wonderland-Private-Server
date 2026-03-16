using System;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// Attack pattern for battle skills - determines which grid positions are hit
    /// </summary>
    public class BattleAttackPattern
    {
        AttackPattern m_pattern;

        public BattleAttackPattern(AttackPattern pattern = AttackPattern.Single)
        {
            m_pattern = pattern;
        }

        public void Set(byte ptr, byte data, byte max)
        {
            m_pattern = (AttackPattern)data;
        }

        public List<byte[]> GetTargets(byte[] targetGrid, BattleRole side)
        {
            List<byte[]> targets = new List<byte[]>();
            switch (m_pattern)
            {
                case AttackPattern.Single:
                    targets.Add(targetGrid);
                    break;
                case AttackPattern.HorizontalLine:
                    targets.Add(targetGrid);
                    targets.Add(new byte[] { targetGrid[0], (byte)(targetGrid[1] - 1) });
                    targets.Add(new byte[] { targetGrid[0], (byte)(targetGrid[1] + 1) });
                    break;
                case AttackPattern.VerticalLine:
                    targets.Add(targetGrid);
                    targets.Add(new byte[] { (byte)(targetGrid[0] - 1), targetGrid[1] });
                    targets.Add(new byte[] { (byte)(targetGrid[0] + 1), targetGrid[1] });
                    break;
            }
            return targets;
        }
    }

    /// <summary>
    /// Minimal skill representation for the battle system.
    /// Wraps the essential data needed by Calculate() without depending on the full Skill/SkillInfo system.
    /// </summary>
    public class BattleSkill
    {
        public ushort SkillID { get; set; }
        public EffectLayer EffectLayer { get; set; }
        public ushort Power { get; set; }
        public byte MaxLevel { get; set; }
        public byte Grade { get; set; }
        public byte NumberOfTurns { get; set; }
        public byte UnknownByte7 { get; set; }
        public ushort SPCost { get; set; }

        BattleAttackPattern m_pattern;

        public BattleSkill()
        {
            m_pattern = new BattleAttackPattern(AttackPattern.Single);
            Grade = 1;
            MaxLevel = 1;
        }

        public ushort AttackPower()
        {
            if (MaxLevel == 0) MaxLevel = 1;
            return (ushort)Math.Floor((double)(((Grade * 100.0) / (double)MaxLevel) / 100.0) * Power);
        }

        public BattleAttackPattern PatternofAttack()
        {
            return m_pattern;
        }

        public void DecreaseTurns()
        {
            if (NumberOfTurns > 0)
                NumberOfTurns--;
        }

        /// <summary>
        /// Creates a basic physical attack skill (normal attack / 普通攻撃)
        /// </summary>
        public static BattleSkill BasicAttack()
        {
            return new BattleSkill
            {
                SkillID = 0,
                EffectLayer = EffectLayer.Physical,
                Power = 10,
                MaxLevel = 1,
                Grade = 1,
                m_pattern = new BattleAttackPattern(AttackPattern.Single)
            };
        }

        /// <summary>
        /// Creates a flee action
        /// </summary>
        public static BattleSkill FleeAction()
        {
            var skill = new BattleSkill
            {
                SkillID = 60046,
                EffectLayer = EffectLayer.Flee,
                Power = 0,
                MaxLevel = 1,
                Grade = 1,
            };
            skill.m_pattern = new BattleAttackPattern(AttackPattern.Single);
            return skill;
        }

        /// <summary>
        /// Creates a BattleSkill from skill data parameters (for bridging with SkillDataFile)
        /// </summary>
        public static BattleSkill FromSkillData(ushort skillID, byte effectLayer, ushort attackPower, byte maxLevel, byte grade,
            byte pattern = 0, byte numberOfTurns = 0, byte unknownByte7 = 0, ushort spCost = 0)
        {
            return new BattleSkill
            {
                SkillID = skillID,
                EffectLayer = (EffectLayer)effectLayer,
                Power = attackPower,
                MaxLevel = maxLevel == 0 ? (byte)1 : maxLevel,
                Grade = grade,
                NumberOfTurns = numberOfTurns,
                UnknownByte7 = unknownByte7,
                SPCost = spCost,
                m_pattern = new BattleAttackPattern((AttackPattern)pattern)
            };
        }

        /// <summary>
        /// Creates a defend action
        /// </summary>
        public static BattleSkill DefendAction()
        {
            return new BattleSkill
            {
                SkillID = 11056,
                EffectLayer = EffectLayer.Defend,
                Power = 0,
                MaxLevel = 1,
                Grade = 1,
                m_pattern = new BattleAttackPattern(AttackPattern.Single)
            };
        }
    }
}
