using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Code;

namespace Server.Bots
{
    /// <summary>
    /// Server-side bot definition. This is the Src/ project version.
    /// The core abstract class lives in wlo.pserver.core/Game/Bots/GmBot.cs
    /// </summary>
    public static class BotManager
    {
        static readonly object m_lock = new object();
        static Dictionary<string, Cupid> m_bots = new Dictionary<string, Cupid>();

        /// <summary>
        /// Register a bot instance.
        /// </summary>
        public static void RegisterBot(string name, Cupid bot)
        {
            lock (m_lock)
            {
                if (!m_bots.ContainsKey(name))
                    m_bots[name] = bot;
            }
        }

        /// <summary>
        /// Get a registered bot by name.
        /// </summary>
        public static Cupid GetBot(string name)
        {
            lock (m_lock)
            {
                if (m_bots.ContainsKey(name))
                    return m_bots[name];
                return null;
            }
        }

        /// <summary>
        /// Initialize default bots.
        /// </summary>
        public static void Initialize()
        {
            RegisterBot("Cupid", new Cupid());
        }
    }
}
