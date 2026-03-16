using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game
{
    /// <summary>
    /// A single mail entry.
    /// </summary>
    public class MailEntry
    {
        public uint SenderCharID;
        public string SenderName;
        public string Message;
        public double Timestamp;
        public ushort AttachedItemID;
        public byte AttachedItemAmount;
        public bool IsRead;

        public MailEntry()
        {
            SenderName = "";
            Message = "";
            Timestamp = DateTime.Now.ToOADate();
        }
    }

    /// <summary>
    /// Manages a player's mailbox.
    ///
    /// AC 14 protocol (mail sub-codes):
    /// Sub 1:  Mail notification (server→client, on receive)
    /// Sub 10: Send mail (client→server)
    /// Sub 11: Request/send mail list
    /// Sub 12: Delete mail
    /// Sub 13: Take attachment from mail
    /// </summary>
    public class MailManager
    {
        Player m_owner;
        List<MailEntry> m_mailbox;
        const int MAX_MAIL = 50;

        /// <summary>
        /// Set by the main project to enable global player lookup.
        /// </summary>
        public static Func<uint, Player> GlobalFindPlayer;

        public MailManager(Player owner)
        {
            m_owner = owner;
            m_mailbox = new List<MailEntry>();
        }

        public int Count { get { return m_mailbox.Count; } }

        /// <summary>
        /// Send mail to another player. If online, delivers immediately.
        /// If offline, mail is stored for next login.
        /// </summary>
        public void SendMail(uint targetCharID, string message, ushort itemID = 0, byte itemAmount = 0)
        {
            MailEntry mail = new MailEntry();
            mail.SenderCharID = m_owner.CharID;
            mail.SenderName = m_owner.CharName;
            mail.Message = message;
            mail.Timestamp = DateTime.Now.ToOADate();
            mail.AttachedItemID = itemID;
            mail.AttachedItemAmount = itemAmount;

            // If attaching an item, remove it from sender's inventory
            if (itemID > 0 && itemAmount > 0)
            {
                byte slot;
                if (!m_owner.Inv.ContainsItem(itemID, out slot))
                    return;
                m_owner.Inv.RemoveItem(slot, itemAmount);
            }

            // Try to deliver to online player
            Player target = null;
            if (GlobalFindPlayer != null)
                target = GlobalFindPlayer(targetCharID);

            if (target != null)
            {
                target.Mail.ReceiveMail(mail);
            }
            // TODO: If offline, store to database for delivery on next login
        }

        /// <summary>
        /// Receive mail into this player's mailbox.
        /// Sends AC 14,1 notification to client.
        /// </summary>
        public void ReceiveMail(MailEntry mail)
        {
            if (m_mailbox.Count >= MAX_MAIL)
                return;

            m_mailbox.Add(mail);

            // Notify client: AC 14,1
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(1);
            pkt.Pack32(mail.SenderCharID);
            pkt.PackStringN(mail.SenderName);
            pkt.PackStringN(mail.Message);
            m_owner.Send(pkt);
        }

        /// <summary>
        /// Send the full mail list to the owner.
        /// AC 14,11 with mail entries.
        /// </summary>
        public void SendMailList()
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(11);
            pkt.Pack8((byte)m_mailbox.Count);

            for (int i = 0; i < m_mailbox.Count; i++)
            {
                var mail = m_mailbox[i];
                pkt.Pack8((byte)i);              // mail index
                pkt.Pack32(mail.SenderCharID);
                pkt.PackStringN(mail.SenderName);
                pkt.PackStringN(mail.Message);
                pkt.Pack8(mail.IsRead ? (byte)1 : (byte)0);
                pkt.Pack16(mail.AttachedItemID);
                pkt.Pack8(mail.AttachedItemAmount);
            }

            m_owner.Send(pkt);
        }

        /// <summary>
        /// Delete a mail by index.
        /// </summary>
        public void DeleteMail(byte index)
        {
            if (index >= m_mailbox.Count)
                return;

            m_mailbox.RemoveAt(index);

            // Confirm deletion: AC 14,12
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(12);
            pkt.Pack8(index);
            m_owner.Send(pkt);
        }

        /// <summary>
        /// Take item attachment from a mail.
        /// </summary>
        public void TakeAttachment(byte index)
        {
            if (index >= m_mailbox.Count)
                return;

            var mail = m_mailbox[index];
            if (mail.AttachedItemID == 0 || mail.AttachedItemAmount == 0)
                return;

            // Add item to player's inventory
            m_owner.Inv.AddItem(mail.AttachedItemID, mail.AttachedItemAmount);

            // Clear attachment
            mail.AttachedItemID = 0;
            mail.AttachedItemAmount = 0;
            mail.IsRead = true;
        }

        /// <summary>
        /// Load mailbox from database string.
        /// Format: senderID|senderName|message|timestamp|itemID|itemAmt|read & (repeated)
        /// </summary>
        public void LoadFromDB(string data)
        {
            m_mailbox.Clear();
            if (string.IsNullOrEmpty(data) || data == "none")
                return;

            string[] entries = data.Split('&');
            foreach (string entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                string[] parts = entry.Split('|');
                if (parts.Length < 7) continue;

                MailEntry mail = new MailEntry();
                uint.TryParse(parts[0], out mail.SenderCharID);
                mail.SenderName = parts[1];
                mail.Message = parts[2];
                double.TryParse(parts[3], out mail.Timestamp);
                ushort.TryParse(parts[4], out mail.AttachedItemID);
                byte.TryParse(parts[5], out mail.AttachedItemAmount);
                mail.IsRead = parts[6] == "1";
                m_mailbox.Add(mail);
            }
        }

        /// <summary>
        /// Save mailbox to database string format.
        /// </summary>
        public string SaveToDB()
        {
            if (m_mailbox.Count == 0)
                return "none";

            var sb = new StringBuilder();
            for (int i = 0; i < m_mailbox.Count; i++)
            {
                var m = m_mailbox[i];
                if (i > 0) sb.Append('&');
                sb.AppendFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                    m.SenderCharID,
                    m.SenderName,
                    m.Message,
                    m.Timestamp,
                    m.AttachedItemID,
                    m.AttachedItemAmount,
                    m.IsRead ? "1" : "0");
            }
            return sb.ToString();
        }
    }
}
