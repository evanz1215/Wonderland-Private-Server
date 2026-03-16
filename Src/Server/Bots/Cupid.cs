using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Bots;
using Game.Code;
using Network;
using Network.ActionCodes;

namespace Server.Bots
{
    /// <summary>
    /// Cupid NPC Bot — Sends periodic announcements and acts as an event host.
    /// Can be placed on a map and interacted with.
    /// </summary>
    public class Cupid : GmBot
    {
        public Cupid()
        {
            CharName = "Cupid";
            CharID = 100;
            LoginMap = 10019;
            CurX = 722;
            CurY = 995;
            NickName = "Event Host";
        }

        public override byte Level
        {
            get { return 220; }
        }

        /// <summary>
        /// Send a server-wide announcement as Cupid.
        /// </summary>
        public void Announce(string message)
        {
            AC02.BroadcastSystemAnnouncement("[Cupid] " + message);
        }
    }
}
