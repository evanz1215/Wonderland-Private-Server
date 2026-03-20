using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;
using Game;
using RCLibrary;

namespace Network.ActionCodes
{
    public class AC06:AC
    {
        public override int ID { get { return 06; } }
        public override void ProcessPkt(Player r, RecievePacket p)
        {
                byte sub = p.Unpack8();
                DebugSystem.Write(string.Format("[AC06] ProcessPkt sub={0} CharID={1}", sub, r.CharID));
                switch (sub)
                {
                    case 1: Recv1(r, p); break;
                    //case 2: Recv2(r, p); break;
                    //case 3: Recv3(r, p); break;
                }
        }
        void Recv1(Player p, RecievePacket g)
        {
            DebugSystem.Write(string.Format("[AC06] Move request CharID={0} MyBattle={1} pktCount={2}",
                p.CharID, p.MyBattle != null ? "IN_BATTLE" : "null", g.Count));
            if (g.Count > 5)
            {
                byte direction = g.Unpack8();
                p.CurX = g.Unpack16();
                p.CurY = g.Unpack16();
                //WORD unknown = p->Unpack16(7);
                //p.Info.e = 0;
                DebugSystem.Write(string.Format("[AC06] Broadcasting move CharID={0} dir={1} x={2} y={3}",
                    p.CharID, direction, p.CurX, p.CurY));
                p.CurMap.Broadcast(Tools.FromFormat("bbdbww", 6, 1, p.CharID, direction, p.CurX, p.CurY));
            }
        }
        void Recv2(Player p, RecievePacket r)
        {
        }
        void Recv3(Player p, RecievePacket r)
        {
        }
    }
}
