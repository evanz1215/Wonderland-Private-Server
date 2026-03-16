using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public enum QuestState : byte
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        TurnedIn = 3,
    }

    /// <summary>
    /// Represents a quest instance tracked per-player.
    /// </summary>
    public class Quest
    {
        public int QID;
        public int progress;
        public int total;

        public QuestState State
        {
            get
            {
                if (progress >= total && total > 0) return QuestState.Completed;
                if (progress > 0) return QuestState.InProgress;
                return QuestState.NotStarted;
            }
        }

        public bool IsComplete { get { return progress >= total && total > 0; } }
    }

    /// <summary>
    /// Defines a quest template loaded from data.
    /// Contains reward info and requirements.
    /// </summary>
    public class QuestTemplate
    {
        public int QuestID;
        public string Name;
        public string Description;

        // Requirements
        public byte MinLevel;
        public int PrerequisiteQuestID;

        // Objectives
        public int TargetNpcID;       // NPC to kill or talk to
        public ushort TargetItemID;   // Item to collect
        public int TargetCount;       // How many to kill/collect

        // Rewards
        public int RewardExp;
        public int RewardGold;
        public ushort RewardItemID;
        public byte RewardItemAmount;

        public QuestTemplate()
        {
            TargetCount = 1;
        }
    }

    /// <summary>
    /// Global quest template registry. Holds all quest definitions.
    /// Load from file or register programmatically.
    /// </summary>
    public class QuestTemplateManager
    {
        Dictionary<int, QuestTemplate> m_templates = new Dictionary<int, QuestTemplate>();
        readonly object m_lock = new object();

        /// <summary>
        /// Register a quest template.
        /// </summary>
        public void Register(QuestTemplate template)
        {
            lock (m_lock)
            {
                m_templates[template.QuestID] = template;
            }
        }

        /// <summary>
        /// Get a quest template by ID.
        /// </summary>
        public QuestTemplate Get(int questID)
        {
            lock (m_lock)
            {
                QuestTemplate t;
                m_templates.TryGetValue(questID, out t);
                return t;
            }
        }

        /// <summary>
        /// Get all registered quest templates.
        /// </summary>
        public List<QuestTemplate> GetAll()
        {
            lock (m_lock) return m_templates.Values.ToList();
        }

        public int Count { get { lock (m_lock) return m_templates.Count; } }

        /// <summary>
        /// Load quest templates from a pipe-delimited text file.
        /// Format per line: QuestID|Name|Description|MinLevel|PrereqQuestID|TargetNpcID|TargetItemID|TargetCount|RewardExp|RewardGold|RewardItemID|RewardItemAmount
        /// Lines starting with # are comments.
        /// </summary>
        public int LoadFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return 0;

            int count = 0;
            foreach (string line in System.IO.File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('|');
                if (parts.Length < 12) continue;

                try
                {
                    var t = new QuestTemplate();
                    t.QuestID = int.Parse(parts[0].Trim());
                    t.Name = parts[1].Trim();
                    t.Description = parts[2].Trim();
                    t.MinLevel = byte.Parse(parts[3].Trim());
                    t.PrerequisiteQuestID = int.Parse(parts[4].Trim());
                    t.TargetNpcID = int.Parse(parts[5].Trim());
                    t.TargetItemID = ushort.Parse(parts[6].Trim());
                    t.TargetCount = int.Parse(parts[7].Trim());
                    t.RewardExp = int.Parse(parts[8].Trim());
                    t.RewardGold = int.Parse(parts[9].Trim());
                    t.RewardItemID = ushort.Parse(parts[10].Trim());
                    t.RewardItemAmount = byte.Parse(parts[11].Trim());

                    Register(t);
                    count++;
                }
                catch { }
            }
            return count;
        }
    }

    /// <summary>
    /// Manages a player's active and completed quests.
    /// </summary>
    public class QuestManager
    {
        Player m_owner;
        List<Quest> m_quests;
        readonly object m_lock = new object();

        public QuestManager(Player owner)
        {
            m_owner = owner;
            m_quests = new List<Quest>();
        }

        public IReadOnlyList<Quest> ActiveQuests
        {
            get { lock (m_lock) return m_quests.Where(q => q.State == QuestState.InProgress).ToList(); }
        }

        public IReadOnlyList<Quest> CompletedQuests
        {
            get { lock (m_lock) return m_quests.Where(q => q.State == QuestState.Completed || q.State == QuestState.TurnedIn).ToList(); }
        }

        public Quest GetQuest(int questID)
        {
            lock (m_lock) return m_quests.FirstOrDefault(q => q.QID == questID);
        }

        public bool HasQuest(int questID)
        {
            lock (m_lock) return m_quests.Any(q => q.QID == questID);
        }

        public bool HasCompletedQuest(int questID)
        {
            lock (m_lock) return m_quests.Any(q => q.QID == questID && (q.State == QuestState.Completed || q.State == QuestState.TurnedIn));
        }

        /// <summary>
        /// Accept a new quest. Returns false if already started.
        /// </summary>
        public bool AcceptQuest(QuestTemplate template)
        {
            if (template == null) return false;
            lock (m_lock)
            {
                if (m_quests.Any(x => x.QID == template.QuestID)) return false;

                // Check prerequisites
                if (template.PrerequisiteQuestID > 0 && !HasCompletedQuest(template.PrerequisiteQuestID))
                    return false;
                if (m_owner.Level < template.MinLevel)
                    return false;

                Quest q = new Quest();
                q.QID = template.QuestID;
                q.progress = 0;
                q.total = template.TargetCount;
                m_quests.Add(q);
                return true;
            }
        }

        /// <summary>
        /// Advance quest progress (e.g. after killing a mob or collecting an item).
        /// </summary>
        public void UpdateProgress(int questID, int amount = 1)
        {
            lock (m_lock)
            {
                var q = m_quests.FirstOrDefault(x => x.QID == questID && x.State == QuestState.InProgress);
                if (q != null)
                {
                    q.progress = Math.Min(q.progress + amount, q.total);
                }
            }
        }

        /// <summary>
        /// Turn in a completed quest and receive rewards.
        /// </summary>
        public bool TurnInQuest(QuestTemplate template)
        {
            if (template == null) return false;
            lock (m_lock)
            {
                var q = m_quests.FirstOrDefault(x => x.QID == template.QuestID && x.State == QuestState.Completed);
                if (q == null) return false;

                // Remove required items if applicable
                if (template.TargetItemID > 0 && template.TargetCount > 0)
                {
                    byte slot;
                    if (!m_owner.Inv.ContainsItem(template.TargetItemID, out slot))
                        return false;
                    m_owner.Inv.RemoveItem(slot, (byte)template.TargetCount);
                }

                // Give rewards
                if (template.RewardExp > 0)
                    m_owner.Eqs.CurExp += template.RewardExp;
                if (template.RewardGold > 0)
                {
                    m_owner.Eqs.AddGold(template.RewardGold);
                    m_owner.Eqs.SendGold();
                }
                if (template.RewardItemID > 0 && template.RewardItemAmount > 0)
                    m_owner.Inv.AddItem(template.RewardItemID, template.RewardItemAmount);

                q.progress = q.total; // ensure marked complete
                return true;
            }
        }

        /// <summary>
        /// Abandon a quest.
        /// </summary>
        public bool AbandonQuest(int questID)
        {
            lock (m_lock)
            {
                var q = m_quests.FirstOrDefault(x => x.QID == questID && x.State == QuestState.InProgress);
                if (q == null) return false;
                m_quests.Remove(q);
                return true;
            }
        }

        /// <summary>
        /// Load quests from database data.
        /// Each entry is [questID, progress, total].
        /// </summary>
        public void LoadFromDB(List<int[]> data)
        {
            lock (m_lock)
            {
                m_quests.Clear();
                foreach (var row in data)
                {
                    if (row.Length >= 2)
                    {
                        Quest q = new Quest();
                        q.QID = row[0];
                        q.progress = row[1];
                        q.total = (row.Length >= 3) ? row[2] : 1;
                        m_quests.Add(q);
                    }
                }
            }
        }

        /// <summary>
        /// Get quest data for DB save.
        /// Returns list of [questID, progress, total].
        /// </summary>
        public List<int[]> GetDBData()
        {
            lock (m_lock)
            {
                return m_quests.Select(q => new int[] { q.QID, q.progress, q.total }).ToList();
            }
        }
    }
}
