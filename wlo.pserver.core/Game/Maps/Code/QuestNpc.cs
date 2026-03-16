using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game.Maps
{
    /// <summary>
    /// NPC that handles quest dialogues and quest interactions.
    ///
    /// WLO NPC Dialog Protocol:
    ///   AC 20,1  → Player clicks NPC (clickID sent)
    ///   Server   → Sends dialog packet via QueueData (AC 20,1 response with dialog text)
    ///   AC 20,6  → Player clicks "next" to continue dialog
    ///   AC 20,9  → Player sends dialog answer (yes/no choice)
    ///
    /// Dialog packets use AC 20 with dialog type bytes to control
    /// what the client displays (text, choices, quest accept, etc).
    /// </summary>
    public class QuestNpc : InteractableObjects
    {
        ushort m_clickID;
        string m_name;
        QuestTemplate m_questTemplate;

        public QuestNpc() { }

        public QuestNpc(ushort clickID, string name, QuestTemplate quest = null)
        {
            m_clickID = clickID;
            m_name = name;
            m_questTemplate = quest;
        }

        public override MapObjType Type { get { return MapObjType.Npc; } }
        public override ushort CickID { get { return m_clickID; } }
        public virtual string Name { get { return m_name ?? ""; } }
        public QuestTemplate QuestData { get { return m_questTemplate; } set { m_questTemplate = value; } }

        /// <summary>
        /// Evaluate what quest data to show this player
        /// (e.g., hide NPC quest markers if already completed)
        /// </summary>
        public virtual void EvaluateQuestData(Player src)
        {
        }

        /// <summary>
        /// Send a dialog message to the player.
        /// Dialog format: AC 20, sub 1, dialog type, text
        /// </summary>
        protected void SendDialog(Player src, byte dialogType, string text)
        {
            SendPacket p = new SendPacket();
            p.Pack8(20);
            p.Pack8(1);
            p.Pack32(1); // dialog context
            p.Pack8(dialogType);
            // dialogType: 7 = simple text, 3 = yes/no choice, 5 = quest accept
            p.Pack8(0);
            p.Pack16(0);
            p.Pack32(0);
            p.Pack8(0);
            p.Pack16(0);
            p.Pack8(1); // event index
            src.QueuePacket(p);
        }

        /// <summary>
        /// Send quest accept/complete dialog to the player.
        /// </summary>
        protected void SendQuestDialog(Player src)
        {
            if (m_questTemplate == null) return;

            var quest = src.Quests.GetQuest(m_questTemplate.QuestID);

            if (quest == null)
            {
                // Player hasn't started this quest — offer it
                SendDialog(src, 5, m_questTemplate.Description ?? "");
            }
            else if (quest.IsComplete)
            {
                // Quest is complete — turn it in
                if (src.Quests.TurnInQuest(m_questTemplate))
                {
                    // Send reward notification
                    SendDialog(src, 7, "Quest complete! Rewards given.");
                }
            }
            else
            {
                // Quest in progress — show progress
                SendDialog(src, 7, "Quest in progress...");
            }
        }

        /// <summary>
        /// Called when a player clicks on this NPC.
        /// </summary>
        public override void Interact(Player src)
        {
            if (m_questTemplate != null)
                SendQuestDialog(src);
            else
                SendDialog(src, 7, "...");

            // End interaction
            src.Send(Tools.FromFormat("bb", 20, 8));
        }

        /// <summary>
        /// Called when a player responds to a dialog choice.
        /// answer: 1 = yes/accept, 0 = no/decline
        /// </summary>
        public override void Interact(Player src, byte? answer = null)
        {
            if (m_questTemplate != null && answer.HasValue)
            {
                if (answer.Value == 1) // Accept
                {
                    if (src.Quests.AcceptQuest(m_questTemplate))
                    {
                        SendDialog(src, 7, "Quest accepted!");
                    }
                }
            }

            src.Send(Tools.FromFormat("bb", 20, 8));
        }

        public override void Interact(Player src, byte? answer = null, params Code.ShoppingCart[] items)
        {
            Interact(src, answer);
        }
    }
}
