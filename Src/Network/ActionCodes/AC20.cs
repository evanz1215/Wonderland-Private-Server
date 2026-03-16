using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;
using Network.ActionCodes;
using Game;
using Game.Maps;

namespace Wonderland_Private_Server.ActionCodes
{
    public class AC20 : AC
    {
        public override int ID { get { return 20; } }
        public override void ProcessPkt(Player r, RecievePacket p)
        {
            switch (p.Unpack8())
            {
                case 1: Recv1(r, p); break;
                case 6: Recv6(r, p); break;
                case 9: Recv9(r, p); break;
                case 8: Recv8(r, p); break;
            }
        }
        void Recv8(Player p, RecievePacket r)
        {
            if (!p.CurMap.Teleport(TeleportType.Regular, p, (byte)r.Unpack16()))
                p.Send(Tools.FromFormat("bb", 20, 8));
        }
        void Recv1(Player p, RecievePacket r)
        {
            ushort clickID = r.Unpack16();

            var map = p.CurMap as GameMap;
            if (map != null)
            {
                // Check if clicked NPC is a shop
                var shop = map.FindShop(clickID);
                if (shop != null)
                {
                    p.InteractingShop = shop;
                    shop.SendShopList(p);
                    p.Send(Tools.FromFormat("bb", 20, 8));
                    return;
                }

                // Check if clicked NPC is a quest NPC
                var questNpc = map.FindQuestNpc(clickID);
                if (questNpc != null)
                {
                    questNpc.Interact(p);
                    return;
                }
            }

            p.Send(Tools.FromFormat("bb", 20, 8));
        }
        void Recv6(Player p, RecievePacket r)
        {
            if (!p.ContinueInteraction())
            {
                p.Send(Tools.FromFormat("bb", 20, 8));

                if (p.Flags.HasFlag(PlayerFlag.Warping))
                {
                    p.Flags.Add(PlayerFlag.InMap);
                    p.Send(Tools.FromFormat("bb", 5, 4));
                }

                //switch (p.State)
                //{
                //    case PlayerState.InGame_Warping:
                //        {
                //            tmp = new SendPacket();
                //            tmp.Pack(new byte[] { 5, 4 });
                //            p.Send(tmp);
                //        } break;
                //    case PlayerState.InGame_Interacting:
                //        {
                //            p.object_interactingwith = null;
                //        } break;
                //}

            }
        }
        void Recv9(Player p, RecievePacket r)
        {
            // Dialog answer from player (e.g., quest accept/decline)
            byte answer = r.Unpack8();

            // Re-interact with the last quest NPC if present
            var map = p.CurMap as GameMap;
            if (map != null)
            {
                // Try to find the quest NPC the player was interacting with
                // For now, iterate quest NPCs — could be optimized with a per-player tracking field
                foreach (var npc in map.QuestNpcsList)
                {
                    if (npc.QuestData != null)
                    {
                        npc.Interact(p, answer);
                        return;
                    }
                }
            }

            p.Send(Tools.FromFormat("bb", 20, 8));
        }
    }
}