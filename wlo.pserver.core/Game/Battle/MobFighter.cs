using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battle
{
    /// <summary>
    /// NPC/Monster battle fighter implementation.
    /// Wraps NPC stat data from the data files for use in the battle system.
    /// </summary>
    public class MobFighter : Fighter
    {
        #region Fields
        uint m_id;
        ushort m_npcID;
        string m_name;
        byte m_level;
        int m_curHP, m_curSP;
        int m_maxHP;
        short m_maxSP;
        int m_atk, m_def, m_matk, m_mdef, m_spd;
        ushort m_baseStr, m_baseCon, m_baseInt, m_baseWis, m_baseAgi;
        Affinity m_element;
        byte m_gridX, m_gridY;
        BattleRole m_battlePosition;
        BattleAction m_action;
        BattleSkill m_skillEffect;
        ushort m_clickID;
        uint m_ownerID;
        DateTime m_rdEndTime;
        BattleScene m_battleRef;

        // NPC skill IDs for AI
        ushort[] m_skillIDs;
        List<BattleSkill> m_skills;

        // Drop items
        ushort[] m_dropItemIDs;

        // EXP/Gold rewards
        uint m_expReward;
        uint m_goldReward;

        // Capture flag
        bool m_catchable;

        static uint s_nextID = 100000;
        static Random s_rng = new Random();

        /// <summary>
        /// Static callback to resolve skill data from SkillManager. Set by main project.
        /// Returns BattleSkill for given skillID, or null if not found.
        /// </summary>
        public static Func<ushort, BattleSkill> ResolveSkill;
        #endregion

        #region Constructor
        /// <summary>
        /// Create a MobFighter from NPC data values
        /// </summary>
        public MobFighter(ushort npcID, string name, byte level,
            uint hp, uint sp,
            ushort str, ushort con, ushort intl, ushort wis, ushort agi, ushort spd,
            byte element,
            ushort[] skillIDs = null, ushort[] dropItemIDs = null, bool catchable = false)
        {
            m_id = s_nextID++;
            m_npcID = npcID;
            m_name = name;
            m_level = level;
            m_curHP = (int)hp;
            m_curSP = (int)sp;
            m_maxHP = (int)hp;
            m_maxSP = (short)sp;

            // Store base stats for pet capture
            m_baseStr = str;
            m_baseCon = con;
            m_baseInt = intl;
            m_baseWis = wis;
            m_baseAgi = agi;

            // Calculate combat stats from base stats
            m_atk = (int)(str * 2.5 + level);
            m_def = (int)(con * 2.0 + level);
            m_matk = (int)(intl * 2.5 + level);
            m_mdef = (int)(wis * 2.0 + level);
            m_spd = (int)(agi * 1.5 + spd);

            m_element = (Affinity)element;
            m_battlePosition = BattleRole.none;

            m_skillIDs = skillIDs ?? new ushort[0];
            m_dropItemIDs = dropItemIDs ?? new ushort[0];
            m_catchable = catchable;

            // Build skill list: always include basic attack, plus any provided skill IDs
            m_skills = new List<BattleSkill>();
            m_skills.Add(BattleSkill.BasicAttack());

            // Resolve additional skills from SkillManager
            if (ResolveSkill != null && m_skillIDs.Length > 0)
            {
                foreach (var sid in m_skillIDs)
                {
                    var resolved = ResolveSkill(sid);
                    if (resolved != null)
                        m_skills.Add(resolved);
                }
            }

            // Calculate EXP/Gold rewards based on level
            m_expReward = (uint)(level * level * 2 + level * 10);
            m_goldReward = (uint)(level * 5 + level * level);
        }
        #endregion

        #region Fighter Interface
        public uint ID { get { return m_npcID; } }
        public BattleRole BattlePosition { get { return m_battlePosition; } set { m_battlePosition = value; } }
        public eFighterType TypeofFighter { get { return eFighterType.Npc_Mob; } }
        public FighterState BattleState
        {
            get
            {
                if (m_curHP <= 0) return FighterState.Dead;
                return FighterState.Alive;
            }
        }
        public BattleAction myAction { get { return m_action; } set { m_action = value; } }
        public UInt16 ClickID { get { return m_clickID; } set { m_clickID = value; } }
        public UInt32 OwnerID { get { return m_ownerID; } set { m_ownerID = value; } }
        public byte Level { get { return m_level; } }
        public byte GridX { get { return m_gridX; } set { m_gridX = value; } }
        public byte GridY { get { return m_gridY; } set { m_gridY = value; } }
        public bool ActionDone { get { return (m_action != null || DateTime.Now > m_rdEndTime); } }
        public Int32 CurHP { get { return m_curHP; } set { m_curHP = value; } }
        public Int32 CurSP { get { return m_curSP; } set { m_curSP = value; } }
        public DateTime RdEndTime { set { m_rdEndTime = value; } }
        public Int32 MaxHP { get { return m_maxHP; } }
        public Int16 MaxSP { get { return m_maxSP; } }
        public Affinity Element { get { return m_element; } }
        public RebornJob Job { get { return RebornJob.none; } }
        public BattleSkill SkillEffect { get { return m_skillEffect; } set { m_skillEffect = value; } }
        public bool Reborn { get { return false; } }
        public Int32 FullMatk { get { return m_matk; } }
        public Int32 FullAtk { get { return m_atk; } }
        public Int32 FullDef { get { return m_def; } }
        public Int32 FullMdef { get { return m_mdef; } }
        public Int32 FullSpd { get { return m_spd; } }

        public void OnNewBattle(BattleScene battle)
        {
            m_battleRef = battle;
            m_action = null;
            m_skillEffect = null;
        }
        #endregion

        #region NPC Properties
        public ushort NpcID { get { return m_npcID; } }
        public string Name { get { return m_name; } }
        public ushort[] DropItemIDs { get { return m_dropItemIDs; } }
        public uint ExpReward { get { return m_expReward; } }
        public uint GoldReward { get { return m_goldReward; } }
        public bool Catchable { get { return m_catchable; } }
        public ushort BaseStr { get { return m_baseStr; } }
        public ushort BaseCon { get { return m_baseCon; } }
        public ushort BaseInt { get { return m_baseInt; } }
        public ushort BaseWis { get { return m_baseWis; } }
        public ushort BaseAgi { get { return m_baseAgi; } }
        #endregion

        #region AI
        /// <summary>
        /// NPC AI - selects skill and target for this round.
        /// Prioritizes low HP targets, uses skills when SP allows.
        /// Called during OnNewRound in BattleScene.
        /// </summary>
        public void ProcessAI(List<Fighter> enemies)
        {
            if (BattleState == FighterState.Dead) return;
            if (enemies == null || enemies.Count == 0) return;

            var aliveEnemies = enemies.Where(e => e.BattleState == FighterState.Alive).ToList();
            if (aliveEnemies.Count == 0) return;

            // Select skill: use a non-basic skill ~40% of the time if SP allows
            BattleSkill skill = BattleSkill.BasicAttack();
            if (m_skills.Count > 1 && s_rng.Next(100) < 40)
            {
                // Pick a random non-basic skill
                var usable = m_skills.Where(s => s.SkillID != 0 && s.SPCost <= m_curSP).ToList();
                if (usable.Count > 0)
                {
                    skill = usable[s_rng.Next(usable.Count)];
                    m_curSP -= skill.SPCost;
                }
            }

            // Select target: 50% chance to target lowest HP enemy
            Fighter target;
            if (s_rng.Next(100) < 50)
            {
                target = aliveEnemies.OrderBy(e => e.CurHP).First();
            }
            else
            {
                target = aliveEnemies[s_rng.Next(aliveEnemies.Count)];
            }

            m_action = new BattleAction
            {
                src = this,
                dst = target,
                skill = skill,
                unknownbyte = 0,
                unknownbyte2 = 0,
            };
        }
        #endregion
    }
}
