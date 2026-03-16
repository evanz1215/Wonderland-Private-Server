using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game;

namespace Server.System
{
    /// <summary>
    /// Logs important game actions to file for monitoring and audit.
    /// Thread-safe with async file writing.
    /// </summary>
    public static class GameLogger
    {
        static readonly ConcurrentQueue<string> m_queue = new ConcurrentQueue<string>();
        static Thread m_writer;
        static bool m_running;
        static string m_logDir;

        /// <summary>
        /// Initialize the game logger with a log directory.
        /// </summary>
        public static void Initialize(string logDir = "logs")
        {
            m_logDir = logDir;
            if (!Directory.Exists(m_logDir))
                Directory.CreateDirectory(m_logDir);

            m_running = true;
            m_writer = new Thread(WriterLoop);
            m_writer.IsBackground = true;
            m_writer.Name = "GameLogger";
            m_writer.Start();
        }

        public static void Stop()
        {
            m_running = false;
            Flush();
        }

        #region Log Methods

        /// <summary>
        /// Log a player login event.
        /// </summary>
        public static void LogLogin(Player p)
        {
            Enqueue("LOGIN", p.CharName + " (ID:" + p.CharID + ") logged in");
        }

        /// <summary>
        /// Log a player logout/disconnect event.
        /// </summary>
        public static void LogLogout(Player p)
        {
            Enqueue("LOGOUT", p.CharName + " (ID:" + p.CharID + ") logged out");
        }

        /// <summary>
        /// Log a chat message.
        /// </summary>
        public static void LogChat(Player p, string channel, string message)
        {
            Enqueue("CHAT", "[" + channel + "] " + p.CharName + ": " + message);
        }

        /// <summary>
        /// Log a trade between two players.
        /// </summary>
        public static void LogTrade(Player p1, Player p2, string details)
        {
            Enqueue("TRADE", p1.CharName + " <-> " + p2.CharName + " | " + details);
        }

        /// <summary>
        /// Log a GM command execution.
        /// </summary>
        public static void LogGMCommand(Player gm, string command)
        {
            Enqueue("GM", gm.CharName + " executed: " + command);
        }

        /// <summary>
        /// Log an item acquisition (drop, shop, GM give).
        /// </summary>
        public static void LogItemGain(Player p, ushort itemID, byte amount, string source)
        {
            Enqueue("ITEM", p.CharName + " gained " + amount + "x item:" + itemID + " from " + source);
        }

        /// <summary>
        /// Log a gold change.
        /// </summary>
        public static void LogGold(Player p, int amount, string reason)
        {
            Enqueue("GOLD", p.CharName + " gold " + (amount >= 0 ? "+" : "") + amount + " (" + reason + ") total:" + p.Gold);
        }

        /// <summary>
        /// Log a level up event.
        /// </summary>
        public static void LogLevelUp(Player p, byte newLevel)
        {
            Enqueue("LEVEL", p.CharName + " reached level " + newLevel);
        }

        /// <summary>
        /// Log a reborn event.
        /// </summary>
        public static void LogReborn(Player p, string jobName)
        {
            Enqueue("REBORN", p.CharName + " reborn as " + jobName);
        }

        /// <summary>
        /// Log a generic warning/suspicious activity.
        /// </summary>
        public static void LogWarning(string message)
        {
            Enqueue("WARN", message);
        }

        /// <summary>
        /// Log a generic info event.
        /// </summary>
        public static void LogInfo(string message)
        {
            Enqueue("INFO", message);
        }

        #endregion

        static void Enqueue(string category, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            m_queue.Enqueue("[" + timestamp + "] [" + category + "] " + message);
        }

        static void Flush()
        {
            string filename = Path.Combine(m_logDir, "game_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            StringBuilder sb = new StringBuilder();
            string line;
            while (m_queue.TryDequeue(out line))
            {
                sb.AppendLine(line);
            }
            if (sb.Length > 0)
            {
                try { File.AppendAllText(filename, sb.ToString()); }
                catch { }
            }
        }

        static void WriterLoop()
        {
            while (m_running)
            {
                if (m_queue.Count > 0)
                    Flush();
                Thread.Sleep(5000); // Flush every 5 seconds
            }
        }
    }
}
