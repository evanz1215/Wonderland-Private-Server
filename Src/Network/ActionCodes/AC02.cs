using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Network;
using Game.Maps;
using RCLibrary.Core.Networking;
using Server.System;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 02 — Chat System
    /// Sub 1: Private whisper
    /// Sub 2: Local/map chat + GM commands
    /// Sub 3: Team chat
    /// Sub 5: World chat
    /// </summary>
    public class AC02 : AC
    {
        public override int ID { get { return 2; } }

        public override void ProcessPkt(Player r, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_Whisper(r, p); break;
                case 2: Recv_LocalChat(r, p); break;
                case 3: Recv_TeamChat(r, p); break;
                case 5: Recv_WorldChat(r, p); break;
                default: DebugSystem.Write("AC 2," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Private whisper: player sends a message to a specific player by name
        /// Packet: [2][1][targetName:stringN][message:stringN]
        /// </summary>
        void Recv_Whisper(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                string targetName = p.UnpackStringN();
                string message = p.UnpackStringN();

                if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(message))
                    return;

                // Find target player across all maps
                Player target = cGlobal.gWorld.FindPlayerByName(targetName);
                if (target == null)
                {
                    // Send "player not found" feedback to sender
                    SendSystemMessage(r, "Player '" + targetName + "' is not online.");
                    return;
                }

                // Send whisper to target: [2][1][senderCharID][senderName][message]
                SendPacket toTarget = new SendPacket();
                toTarget.Pack8(2);
                toTarget.Pack8(1);
                toTarget.Pack32(r.CharID);
                toTarget.PackStringN(r.CharName);
                toTarget.PackStringN(message);
                target.Send(toTarget);

                // Echo back to sender for confirmation: [2][1][targetCharID][targetName][message]
                SendPacket toSender = new SendPacket();
                toSender.Pack8(2);
                toSender.Pack8(1);
                toSender.Pack32(target.CharID);
                toSender.PackStringN(target.CharName);
                toSender.PackStringN(message);
                r.Send(toSender);
            }
            catch (Exception ex) { log.Error(ex.Message, ex); }
        }

        /// <summary>
        /// Local/map chat: broadcast message to all players on same map.
        /// Also handles GM commands (:item, :warp).
        /// Packet: [2][2][message:stringN]
        /// </summary>
        void Recv_LocalChat(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                string str = p.UnpackStringN();
                string[] words = str.Split(' ');
                if (words.Length >= 1)
                {
                    // Log all GM commands
                    if (words[0].StartsWith(":"))
                        GameLogger.LogGMCommand(r, str);

                    switch (words[0])
                    {
                        #region item
                        case ":item":
                            {
                                switch (words[1])
                                {
                                    case "add":
                                        {
                                            byte ammt = 1;
                                            int ct = words.Length;
                                            UInt16 itemid = 0;
                                            if (ct > 2)
                                            {
                                                try { itemid = UInt16.Parse(words[2]); }
                                                catch { }
                                                if (ct > 3)
                                                {
                                                    try { ammt = byte.Parse(words[3]); }
                                                    catch { }
                                                }

                                                switch (itemid)
                                                {
                                                    case 34076:
                                                        {
                                                            if (!r.Inv.ContainsItem(34076) && !r.Eqs.IsEquipped(34076))
                                                                r.Inv.AddItem(itemid, ammt);
                                                        }
                                                        break;
                                                    default: r.Inv.AddItem(itemid, ammt); break;
                                                }
                                            }
                                        } break;
                                } break;
                            } break;
                        #endregion
                        #region warp
                        case ":warp":
                            {
                                try
                                {
                                    WarpData tmp = new WarpData();
                                    tmp.DstMap = ushort.Parse(words[1]);
                                    tmp.DstX_Axis = ushort.Parse(words[2]);
                                    tmp.DstY_Axis = ushort.Parse(words[3]);
                                    r.CurMap.Teleport(TeleportType.CmD, r, (byte)0, tmp);
                                }
                                catch { }
                            } break;
                        #endregion
                        #region announce
                        case ":announce":
                            {
                                if (words.Length > 1)
                                {
                                    string msg = string.Join(" ", words, 1, words.Length - 1);
                                    BroadcastSystemAnnouncement(msg);
                                }
                            } break;
                        #endregion
                        #region kick
                        case ":kick":
                            {
                                if (words.Length > 1)
                                {
                                    string targetName = words[1];
                                    Player target = cGlobal.gWorld.FindPlayerByName(targetName);
                                    if (target != null)
                                    {
                                        target.Disconnect();
                                        SendSystemMessage(r, "Kicked player: " + targetName);
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + targetName);
                                }
                            } break;
                        #endregion
                        #region mute
                        case ":mute":
                            {
                                if (words.Length > 1)
                                {
                                    string targetName = words[1];
                                    int minutes = 10;
                                    if (words.Length > 2) try { minutes = int.Parse(words[2]); } catch { }

                                    Player target = cGlobal.gWorld.FindPlayerByName(targetName);
                                    if (target != null)
                                    {
                                        target.Mute(minutes);
                                        SendSystemMessage(r, "Muted " + targetName + " for " + minutes + " minutes.");
                                        SendSystemMessage(target, "You have been muted for " + minutes + " minutes.");
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + targetName);
                                }
                            } break;
                        case ":unmute":
                            {
                                if (words.Length > 1)
                                {
                                    Player target = cGlobal.gWorld.FindPlayerByName(words[1]);
                                    if (target != null)
                                    {
                                        target.Unmute();
                                        SendSystemMessage(r, "Unmuted " + words[1]);
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + words[1]);
                                }
                            } break;
                        #endregion
                        #region goto / summon
                        case ":goto":
                            {
                                if (words.Length > 1)
                                {
                                    Player target = cGlobal.gWorld.FindPlayerByName(words[1]);
                                    if (target != null)
                                    {
                                        WarpData wd = new WarpData();
                                        wd.DstMap = (ushort)target.CurMap.MapID;
                                        wd.DstX_Axis = target.CurX;
                                        wd.DstY_Axis = target.CurY;
                                        r.CurMap.Teleport(TeleportType.CmD, r, 0, wd);
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + words[1]);
                                }
                            } break;
                        case ":summon":
                            {
                                if (words.Length > 1)
                                {
                                    Player target = cGlobal.gWorld.FindPlayerByName(words[1]);
                                    if (target != null)
                                    {
                                        WarpData wd = new WarpData();
                                        wd.DstMap = (ushort)r.CurMap.MapID;
                                        wd.DstX_Axis = r.CurX;
                                        wd.DstY_Axis = r.CurY;
                                        target.CurMap.Teleport(TeleportType.CmD, target, 0, wd);
                                        SendSystemMessage(r, "Summoned " + words[1]);
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + words[1]);
                                }
                            } break;
                        #endregion
                        #region gold
                        case ":gold":
                            {
                                if (words.Length > 1)
                                {
                                    try
                                    {
                                        int amount = int.Parse(words[1]);
                                        r.AddGold(amount);
                                        r.SendGold();
                                        SendSystemMessage(r, "Added " + amount + " gold.");
                                    }
                                    catch { }
                                }
                            } break;
                        #endregion
                        #region level
                        case ":level":
                            {
                                if (words.Length > 1)
                                {
                                    try
                                    {
                                        int targetLevel = int.Parse(words[1]);
                                        if (targetLevel < 1) targetLevel = 1;
                                        if (targetLevel > 199) targetLevel = 199;
                                        // Set total exp to reach this level
                                        long neededExp = 0;
                                        byte rebornFlag = (byte)(r.Reborn ? 1 : 0);
                                        for (int lvl = 1; lvl < targetLevel; lvl++)
                                        {
                                            neededExp += (int)Math.Round(Math.Pow(lvl + 1, (rebornFlag == 0) ? 3.1 : 3.3) + (rebornFlag == 0 ? 5 : 50));
                                        }
                                        r.TotalExp = neededExp;
                                        r.Send8_1(true);
                                        SendSystemMessage(r, "Level set to " + targetLevel);
                                    }
                                    catch { }
                                }
                            } break;
                        #endregion
                        #region heal
                        case ":heal":
                            {
                                r.FillHP();
                                r.FillSP();
                                r.Send8_1();
                                SendSystemMessage(r, "Fully healed.");
                            } break;
                        #endregion
                        #region info
                        case ":info":
                            {
                                if (words.Length > 1)
                                {
                                    Player target = cGlobal.gWorld.FindPlayerByName(words[1]);
                                    if (target != null)
                                    {
                                        string info = "[" + target.CharName + "] Lv." + target.Level
                                            + " Map:" + target.CurMap.MapID
                                            + " (" + target.CurX + "," + target.CurY + ")"
                                            + " HP:" + target.CurHP + "/" + target.FullHP
                                            + " Gold:" + target.Gold
                                            + (target.Reborn ? " Job:" + target.Job : "")
                                            + (target.IsMuted ? " [MUTED]" : "");
                                        SendSystemMessage(r, info);
                                    }
                                    else
                                        SendSystemMessage(r, "Player not found: " + words[1]);
                                }
                                else
                                {
                                    string info = "[" + r.CharName + "] Lv." + r.Level
                                        + " Map:" + r.CurMap.MapID
                                        + " (" + r.CurX + "," + r.CurY + ")"
                                        + " HP:" + r.CurHP + "/" + r.FullHP
                                        + " Gold:" + r.Gold;
                                    SendSystemMessage(r, info);
                                }
                            } break;
                        #endregion
                        #region online
                        case ":online":
                            {
                                SendSystemMessage(r, "Online players: " + cGlobal.gWorld.OnlineCount);
                            } break;
                        #endregion
                        #region event
                        case ":event":
                            {
                                if (words.Length > 1)
                                {
                                    switch (words[1])
                                    {
                                        case "exp":
                                            {
                                                int mins = 60;
                                                if (words.Length > 2) try { mins = int.Parse(words[2]); } catch { }
                                                cGlobal.gWorldEvents.StartDoubleExp(mins);
                                            } break;
                                        case "drop":
                                            {
                                                int mins = 60;
                                                if (words.Length > 2) try { mins = int.Parse(words[2]); } catch { }
                                                cGlobal.gWorldEvents.StartDoubleDrop(mins);
                                            } break;
                                        case "status":
                                            {
                                                SendSystemMessage(r, cGlobal.gWorldEvents.GetStatus());
                                            } break;
                                        default:
                                            SendSystemMessage(r, "Usage: :event [exp|drop|status] [minutes]");
                                            break;
                                    }
                                }
                                else
                                    SendSystemMessage(r, "Usage: :event [exp|drop|status] [minutes]");
                            } break;
                        #endregion
                        #region exp
                        case ":exp":
                            {
                                if (words.Length > 1)
                                {
                                    try
                                    {
                                        int expAmount = int.Parse(words[1]);
                                        r.CurExp = expAmount;
                                        SendSystemMessage(r, "Added " + expAmount + " EXP.");
                                    }
                                    catch { }
                                }
                            } break;
                        #endregion
                        #region quest
                        case ":quest":
                            {
                                if (words.Length > 1)
                                {
                                    switch (words[1])
                                    {
                                        case "add":
                                            {
                                                if (words.Length > 2)
                                                {
                                                    try
                                                    {
                                                        int qid = int.Parse(words[2]);
                                                        var template = cGlobal.gQuestTemplates.Get(qid);
                                                        if (template != null)
                                                        {
                                                            if (r.Quests.AcceptQuest(template))
                                                                SendSystemMessage(r, "Quest accepted: " + template.Name);
                                                            else
                                                                SendSystemMessage(r, "Cannot accept quest " + qid);
                                                        }
                                                        else
                                                            SendSystemMessage(r, "Quest not found: " + qid);
                                                    }
                                                    catch { }
                                                }
                                            } break;
                                        case "complete":
                                            {
                                                if (words.Length > 2)
                                                {
                                                    try
                                                    {
                                                        int qid = int.Parse(words[2]);
                                                        var template = cGlobal.gQuestTemplates.Get(qid);
                                                        if (template != null)
                                                        {
                                                            var q = r.Quests.GetQuest(qid);
                                                            if (q != null)
                                                            {
                                                                r.Quests.UpdateProgress(qid, q.total);
                                                                SendSystemMessage(r, "Quest completed: " + template.Name);
                                                            }
                                                            else
                                                                SendSystemMessage(r, "Quest not active: " + qid);
                                                        }
                                                        else
                                                            SendSystemMessage(r, "Quest not found: " + qid);
                                                    }
                                                    catch { }
                                                }
                                            } break;
                                        case "list":
                                            {
                                                var active = r.Quests.ActiveQuests;
                                                if (active.Count == 0)
                                                    SendSystemMessage(r, "No active quests.");
                                                else
                                                    foreach (var q in active)
                                                        SendSystemMessage(r, "Quest " + q.QID + ": " + q.progress + "/" + q.total);
                                            } break;
                                        case "reload":
                                            {
                                                int cnt = cGlobal.gQuestTemplates.LoadFromFile("Data\\quests.txt");
                                                SendSystemMessage(r, "Reloaded " + cnt + " quest templates.");
                                            } break;
                                        default:
                                            SendSystemMessage(r, "Usage: :quest [add|complete|list|reload] [id]");
                                            break;
                                    }
                                }
                                else
                                    SendSystemMessage(r, "Usage: :quest [add|complete|list|reload] [id]");
                            } break;
                        #endregion
                        #region Default — local broadcast
                        default:
                            {
                                if (r.IsMuted)
                                {
                                    SendSystemMessage(r, "You are muted.");
                                    break;
                                }

                                PacketBuilder tmp = new PacketBuilder();
                                tmp.Begin();
                                tmp.Add((byte)2);
                                tmp.Add((byte)2);
                                tmp.Add(r.CharID);
                                tmp.Add(str, true);

                                r.CurMap.Broadcast(new SendPacket(tmp.End()), "Ex", r.CharID);
                            }
                            break;
                        #endregion
                    }
                }
            }
            catch (Exception t) { log.Error(t.Message, t); }
        }

        /// <summary>
        /// Team chat: broadcast message to all team members
        /// Packet: [2][3][message:stringN]
        /// </summary>
        void Recv_TeamChat(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                string message = p.UnpackStringN();

                if (!r.hasParty || string.IsNullOrEmpty(message))
                    return;

                // Build team chat packet: [2][3][senderCharID][message]
                SendPacket pkt = new SendPacket();
                pkt.Pack8(2);
                pkt.Pack8(3);
                pkt.Pack32(r.CharID);
                pkt.PackStringN(message);

                // Send to all team members including sender
                foreach (var member in r.TeamMembers)
                {
                    member.Send(pkt);
                }
            }
            catch (Exception ex) { log.Error(ex.Message, ex); }
        }

        /// <summary>
        /// World chat: broadcast message to all players on server
        /// Packet: [2][5][message:stringN]
        /// </summary>
        void Recv_WorldChat(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                string message = p.UnpackStringN();

                if (string.IsNullOrEmpty(message))
                    return;

                if (r.IsMuted)
                {
                    SendSystemMessage(r, "You are muted.");
                    return;
                }

                // Build world chat packet: [2][5][senderCharID][message]
                SendPacket pkt = new SendPacket();
                pkt.Pack8(2);
                pkt.Pack8(5);
                pkt.Pack32(r.CharID);
                pkt.PackStringN(message);

                cGlobal.gWorld.BroadcastAll(pkt);
            }
            catch (Exception ex) { log.Error(ex.Message, ex); }
        }

        /// <summary>
        /// Send a system message to a specific player (server → client)
        /// Uses sub 6 for system messages
        /// </summary>
        public static void SendSystemMessage(Player target, string message)
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(2);
            pkt.Pack8(6);
            pkt.PackStringN(message);
            target.Send(pkt);
        }

        /// <summary>
        /// Broadcast a system announcement to all players on the server
        /// </summary>
        public static void BroadcastSystemAnnouncement(string message)
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(2);
            pkt.Pack8(6);
            pkt.PackStringN(message);
            cGlobal.gWorld.BroadcastAll(pkt);
        }
    }
}
