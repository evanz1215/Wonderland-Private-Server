using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Network
{
    /// <summary>
    /// Logs all client packets to file for debugging.
    /// Writes to packet_YYYY-MM-DD.log in the logs directory.
    /// Thread-safe with async file writing.
    /// </summary>
    public static class PacketLogger
    {
        static readonly ConcurrentQueue<string> m_queue = new ConcurrentQueue<string>();
        static Thread m_writer;
        static bool m_running;
        static string m_logDir;
        static bool m_enabled;

        /// <summary>
        /// Set of AC codes to ignore (e.g. movement spam).
        /// Add AC codes here to reduce noise.
        /// </summary>
        static readonly HashSet<int> IgnoredACs = new HashSet<int>
        {
            // AC 6 = movement updates (very spammy)
            // Uncomment to ignore: 6,
        };

        public static bool Enabled
        {
            get { return m_enabled; }
            set { m_enabled = value; }
        }

        public static void Initialize(string logDir = "logs")
        {
            m_logDir = logDir;
            if (!Directory.Exists(m_logDir))
                Directory.CreateDirectory(m_logDir);

            m_enabled = true;
            m_running = true;
            m_writer = new Thread(WriterLoop);
            m_writer.IsBackground = true;
            m_writer.Name = "PacketLogger";
            m_writer.Start();
        }

        public static void Stop()
        {
            m_running = false;
            Flush();
        }

        /// <summary>
        /// Log a received packet from client.
        /// </summary>
        /// <param name="playerInfo">Player identifier (e.g. "CharName(ID)")</param>
        /// <param name="raw">Raw packet bytes</param>
        /// <param name="handled">Whether an AC handler was found</param>
        public static void LogRecv(string playerInfo, byte[] raw, bool handled)
        {
            if (!m_enabled || raw == null || raw.Length < 5) return;

            byte ac = raw[4];
            if (IgnoredACs.Contains(ac)) return;

            byte sub = (raw.Length > 5) ? raw[5] : (byte)0;
            int dataLen = raw.Length - 4; // excluding header (2 bytes magic + 2 bytes length)

            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
            sb.Append(handled ? " RECV  " : " RECV? ");
            sb.AppendFormat("[AC {0,2},{1,-3}] len={2,-4} ", ac, sub, dataLen);
            sb.Append("from ");
            sb.Append(playerInfo);

            // Hex dump of data portion (skip 4-byte header), max 64 bytes
            sb.Append(" | ");
            int dumpLen = Math.Min(raw.Length, 68); // 4 header + 64 data
            for (int i = 4; i < dumpLen; i++)
            {
                sb.AppendFormat("{0:X2} ", raw[i]);
            }
            if (raw.Length > 68)
                sb.Append("...");

            m_queue.Enqueue(sb.ToString());
        }

        /// <summary>
        /// Log a sent packet to client.
        /// </summary>
        public static void LogSend(string playerInfo, byte[] raw)
        {
            if (!m_enabled || raw == null || raw.Length < 5) return;

            byte ac = raw[4];
            if (IgnoredACs.Contains(ac)) return;

            byte sub = (raw.Length > 5) ? raw[5] : (byte)0;
            int dataLen = raw.Length - 4;

            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
            sb.AppendFormat(" SEND  [AC {0,2},{1,-3}] len={2,-4} ", ac, sub, dataLen);
            sb.Append("to ");
            sb.Append(playerInfo);

            sb.Append(" | ");
            int dumpLen = Math.Min(raw.Length, 68);
            for (int i = 4; i < dumpLen; i++)
            {
                sb.AppendFormat("{0:X2} ", raw[i]);
            }
            if (raw.Length > 68)
                sb.Append("...");

            m_queue.Enqueue(sb.ToString());
        }

        /// <summary>
        /// Log an unhandled AC code (no handler found).
        /// </summary>
        public static void LogUnhandled(string playerInfo, byte ac, byte sub, int len)
        {
            if (!m_enabled) return;

            string line = string.Format("{0} !!!!! [AC {1},{2}] UNHANDLED len={3} from {4}",
                DateTime.Now.ToString("HH:mm:ss.fff"), ac, sub, len, playerInfo);
            m_queue.Enqueue(line);
        }

        static void Flush()
        {
            string filename = Path.Combine(m_logDir, "packet_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
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
                Thread.Sleep(2000); // Flush every 2 seconds
            }
        }
    }
}
