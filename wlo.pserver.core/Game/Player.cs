using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//using Server.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RCLibrary.Core.Networking;
using Network;
using Game.Code;
using Game.Maps;

    namespace Game
    {
        public delegate void PlayerSocketInfo(Player src);
        
        public class PlayerFlagManager
        {
            private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            List<PlayerFlag> m_Flags;

            public PlayerFlagManager()
            {
                m_Flags = new List<PlayerFlag>();
            }

            public void Add(params PlayerFlag[] flag)
            {
                foreach (var f in flag)
                    if (!m_Flags.Contains(f))
                        m_Flags.Add(f);
            }
            public void Remove(params PlayerFlag[] flag)
            {
                foreach (var f in flag)
                    if (m_Flags.Contains(f))
                        m_Flags.Remove(f);
            }
            public bool HasFlag(PlayerFlag flag)
            {
                return m_Flags.Contains(flag);
            }
        }
        

        public class Player : Game.Character, IDisposable, INotifyPropertyChanged, Game.Battle.Fighter
        {
            private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            #region Events
            public event PlayerSocketInfo Disconnected;
            /// <summary>
            /// Static callback for level-up logging. Set by the main project (e.g., WorldServer).
            /// </summary>
            public static Action<Player, byte> OnPlayerLevelUp;

            /// <summary>
            /// Global EXP multiplier (e.g., 2.0 for double exp events). Set by WorldEventSystem.
            /// </summary>
            public static Func<double> GetExpMultiplier;

            /// <summary>
            /// Static callback to resolve quest template by ID. Set by main project.
            /// </summary>
            public static Func<int, QuestTemplate> GetQuestTemplate;

            #endregion

            #region Definitions

            readonly object mlock = new object();

            SocketClient m_socket;
            Thread net;

            PlayerFlagManager m_Flags;

            Queue<SendPacket> QueueData;
            SendMode m_sendMode;

            WarpData prevMap;
            WarpData returnSpawnMap;
            WarpData recordMap;
            WarpData gpsMap;

            int slot;
            byte emote;

            // Battle fields
            BattleRole m_battlePosition;
            Game.Battle.BattleAction m_battleAction;
            Game.Battle.BattleSkill m_skillEffect;
            byte m_gridX, m_gridY;
            UInt16 m_clickID;
            UInt32 m_ownerID;
            DateTime m_rdEndTime;

            User m_useracc;
            Inventory m_inv;
            ClientSettings m_settings;
            Game.Battle.BattleScene m_battle;
            Game.Maps.ShopKeeper m_interactingShop;
            QuestManager m_questManager;
            TeamManager m_team;
            TradeManager m_trade;
            FriendManager m_friends;
            MailManager m_mail;
            Game.Code.PetRelated.PetList m_petlist;
            //RiceBall m_riceball;
            Tent m_tent;
            Guild m_guild;
            int m_curInstance;
            DateTime m_muteUntil = DateTime.MinValue;
            #endregion


            public Player(SocketClient src,DataFiles.PhxItemDat itemdat)
                : base(src.SendPacket,itemdat)
            {
                m_socket = src;
                m_socket.onConnectionLost += m_socket_onConnectionLost;
                m_socket.onPacketRecved = ProcessSocket;
                QueueData = new Queue<SendPacket>(25);

                
                m_inv = new Inventory(this,itemdat);
                onWearEquip = m_inv.onWearEquip;
                onEquip_Remove = m_inv.onUnEquip;
                m_tent = new Tent(this);
                m_questManager = new QuestManager(this);
                m_team = new TeamManager(this);
                m_trade = new TradeManager(this);
                m_friends = new FriendManager(this);
                m_mail = new MailManager(this);
                m_petlist = new Game.Code.PetRelated.PetList(this);

                m_useracc = new User();
                Flags = new PlayerFlagManager();
                m_settings = new ClientSettings();
                //m_friendlist = new Friendlist(new Action<SendPacket>(SendPacket));
                //m_Mail = new MailManager(this);
                
                //m_petlist = new PetList(this);

                while (m_socket.m_IncomingPackets.Count > 0)
                {
                    IPacket p;
                    m_socket.m_IncomingPackets.TryDequeue(out p);
                    ProcessSocket(p);

                }


            }
            ~Player()
            {
            }


            public void Dispose()
            {
                m_useracc = null;
            }

            public override void Clear()
            {
                m_Flags = new PlayerFlagManager();
                m_inv.RemoveAll(true);
                QueueData = new Queue<SendPacket>(25);
                UserAcc.Clear();
                base.Clear();
            }

            #region ThreadSafe  Properties

            #region Socket
            public TimeSpan TimeIdle { get { return m_socket.Elapsed(); } }
            public bool isDisconnected() { return m_socket.isDisconnected(); }
            public String SockAddress() { return m_socket.SockAddress(); }
            public String LocalPort() { return m_socket.LocalPort(); }
            #endregion

            #region User Account
            public User UserAcc { get { return m_useracc; } }
            //public bool GM { get { return m_gmlvl > 0; } }
            //public bool Busy { get; set; }
            #endregion

            #region Player
            
            
            public PlayerFlagManager Flags
            {
                get
                {
                    lock (mlock) return m_Flags;
                }
                set
                {
                    lock (mlock) m_Flags = value;
                }
            }
            public override uint CharID
            {
                get
                {
                    return (Slot == 1) ? UserAcc.Character1ID : UserAcc.Character2ID;
                }
                set
                {
                    base.CharID = value;
                }
            }
            //public bool BlockSave { get; set; }
            //public bool inGame { get; set; }
            //public PlayerState State
            //{
            //    get
            //    {
            //        if (m_battle != null)
            //        {
            //            if (CurHP != 0)
            //                return PlayerState.InGame_Battling_Alive;
            //        }

            //        return m_state;
            //    }
            //    set
            //    {
            //        m_state = value;
            //    }
            //}
            //public IReadOnlyList<Quest> Completed_Quest { get { return m_started_Quests.Where(c => c.progress == c.total).ToList(); } }
            public ClientSettings Settings
            {
                get
                {
                    return m_settings;
                }
            }
            //public List<Character> Friends
            //{
            //    get
            //    {
            //        return m_friends;
            //    }
            //}
            //public List<Mail> MailBox
            //{
            //    get
            //    {
            //        return mailBox;
            //    }
            //}
            public Inventory Inv { get { return m_inv ?? null; } }
            public EquipManager Eqs { get { return ((EquipManager)this) ?? null; } }
            public byte Emote { get { lock (mlock)return emote; } set { lock (mlock)emote = value; } }
            //public cPetList Pets { get { return m_pets; } }
            public Tent Tent { get { return m_tent; } }
            public Guild CurGuild { get { return m_guild; } set { m_guild = value; } }
            public int CurInstance { get { return m_curInstance; } set { m_curInstance = value; } }
            public bool IsMuted { get { return DateTime.Now < m_muteUntil; } }
            public void Mute(int minutes) { m_muteUntil = DateTime.Now.AddMinutes(minutes); }
            public void Unmute() { m_muteUntil = DateTime.MinValue; }

            /// <summary>
            /// Called when the player levels up. Broadcasts level-up effect to map.
            /// </summary>
            protected override void OnLevelUp(byte newLevel)
            {
                // Invoke logging callback (set by main project)
                if (OnPlayerLevelUp != null)
                    OnPlayerLevelUp(this, newLevel);

                if (CurMap == null) return;

                // Broadcast level-up visual effect to all players on the map
                // AC 8,2 is used for level-up notification: [8][2][charID][newLevel]
                SendPacket pkt = new SendPacket();
                pkt.Pack8(8);
                pkt.Pack8(2);
                pkt.Pack32(CharID);
                pkt.Pack8(newLevel);
                CurMap.Broadcast(pkt);
            }
            public Game.Battle.BattleScene MyBattle { get { return m_battle; } set { m_battle = value; } }
            public Game.Maps.ShopKeeper InteractingShop { get { return m_interactingShop; } set { m_interactingShop = value; } }
            public QuestManager Quests { get { return m_questManager; } }
            public TeamManager Team { get { return m_team; } }
            public TradeManager Trade { get { return m_trade; } }
            public FriendManager Friends { get { return m_friends; } }
            public MailManager Mail { get { return m_mail; } }
            public Game.Code.PetRelated.PetList Pets { get { return m_petlist; } }

            /// <summary>
            /// Get equipped item by slot (1-6). Returns the Equip object.
            /// </summary>
            public Equip GetEquip(byte slot) { return this[slot]; }
            //public cRiceBall RiceBall { get { return m_riceball; } }
            //public SendType DataOut
            //{
            //    get { return dataout; }
            //    set
            //    {
            //        prevdataout = dataout; dataout = value;
            //        if (prevdataout == SendType.Multi && value == SendType.Normal) Send(MultiPkt);
            //        else if (value == SendType.Multi) MultiPkt = new SendPacket(false, true);

            //    }
            //}
            //public override string CharacterName
            //{
            //    get
            //    {
            //        return (this.GM) ? GMStuff.Name + " " + base.CharacterName : base.CharacterName;
            //    }
            //    set
            //    {
            //        base.CharacterName = value;
            //    }
            //}
            public override IMap CurMap
            {
                get
                {
                    return base.CurMap;
                }
                set
                {
                    if (base.CurMap != null)
                    {
                        if (base.CurMap is GameMap)
                        {
                            prevMap = new WarpData();
                            prevMap.DstMap = (ushort)base.CurMap.MapID;
                            prevMap.DstX_Axis = CurX;
                            prevMap.DstY_Axis = CurY;

                            (base.CurMap as GameMap).onItemDropped_fromMap = null;
                            (base.CurMap as GameMap).onItemPickup_fromMap = null;
                        }
                    }
                    base.CurMap = value;
                    if (base.CurMap is GameMap)
                    {
                        (base.CurMap as GameMap).onItemDropped_fromMap = m_inv.onItemDropped_fromMap;
                        (base.CurMap as GameMap).onItemPickup_fromMap = m_inv.onItemPickedUp_fromMap;
                    }
                }
            }

            public WarpData PrevMap
            {
                get { lock (mlock) return prevMap; }
                set { lock (mlock)prevMap = value; }
            }

            #endregion

            #region Fighter Interface
            public uint ID { get { return CharID; } }
            public BattleRole BattlePosition { get { return m_battlePosition; } set { m_battlePosition = value; } }
            public eFighterType TypeofFighter { get { return eFighterType.player; } }
            public FighterState BattleState
            {
                get
                {
                    if (CurHP <= 0) return FighterState.Dead;
                    return FighterState.Alive;
                }
            }
            public Game.Battle.BattleAction myAction { get { return m_battleAction; } set { m_battleAction = value; } }
            public UInt16 ClickID { get { return m_clickID; } set { m_clickID = value; } }
            public UInt32 OwnerID { get { return m_ownerID; } set { m_ownerID = value; } }
            public byte GridX { get { return m_gridX; } set { m_gridX = value; } }
            public byte GridY { get { return m_gridY; } set { m_gridY = value; } }
            public bool ActionDone { get { return (m_battleAction != null || DateTime.Now > m_rdEndTime); } }
            public DateTime RdEndTime { set { m_rdEndTime = value; } }
            public Int32 MaxHP { get { return FullHP; } }
            public Int16 MaxSP { get { return (short)FullSP; } }
            public Game.Battle.BattleSkill SkillEffect { get { return m_skillEffect; } set { m_skillEffect = value; } }

            public override int CurHP
            {
                get { return base.CurHP; }
                set { base.CurHP = value; }
            }
            public override int CurSP
            {
                get { return base.CurSP; }
                set { base.CurSP = value; }
            }

            public void OnNewBattle(Game.Battle.BattleScene battle)
            {
                MyBattle = battle;
                m_battleAction = null;
                m_skillEffect = null;
                m_rdEndTime = DateTime.Now.AddSeconds(20);
            }
            #endregion

            #region Team
            public bool PartyLeader { get { return m_team.IsLeader; } }
            public List<Player> TeamMembers { get { return m_team.TeamMembers; } }
            public bool hasParty { get { return m_team.HasParty; } }
            public SendPacket _13_6Data { get { return m_team.GetTeamDataPacket(); } }
            #endregion

            #endregion

            #region ThreadSafe  Methods

            #region ClientSock

            /// <summary>
            /// Send Player a packet
            /// </summary>
            /// <param name="src"></param>
            public void Send(SendPacket src)
            {
                if (src.Flags == PacketFlags.Queued || src.Flags == PacketFlags.Queue_Dc)
                {
                    src.Flags -= PacketFlags.Queued;
                    QueueData.Enqueue(src);
                    return;
                }
                Send(src, src.Flags);
            }
            public void Send(byte[] src)
            {

            }
            public void Send(byte[] src, RCLibrary.Core.Networking.PacketFlags pFlags)
            {
                SendPacket p = new SendPacket(src);
                Send(p, pFlags);
            }
            public void Send(SendPacket p, RCLibrary.Core.Networking.PacketFlags pFlags)
            {
                if (pFlags == PacketFlags.Queued || pFlags == PacketFlags.Queue_Dc)
                {
                    p.Flags -= PacketFlags.Queued;
                    QueueData.Enqueue(p);
                    return;
                }
                p.Flags = pFlags;
                m_socket.SendPacket(p);

            }
            
            /// <summary>
            /// Queue a packet for dialog/interaction flow (sent via ContinueInteraction)
            /// </summary>
            public void QueuePacket(SendPacket p)
            {
                QueueData.Enqueue(p);
            }

            public void ProcessSocket(IPacket g)
            {
                try
                {
                    byte[] raw;
                    try
                    {
                        raw = g.Buffer.ToArray();
                    }
                    catch
                    {
                        DebugSystem.Write("Recv invalid buffer from " + SockAddress() + ", ignoring");
                        return;
                    }

                    if (raw == null || raw.Length < 5)
                    {
                        DebugSystem.Write("Recv short packet from " + SockAddress() + " len=" + (raw == null ? 0 : raw.Length) + ", ignoring");
                        return;
                    }

                    DebugSystem.Write(DebugItemType.Network_Heavy, "Recv Data from {0} len={1} AC={2}", SockAddress(), raw.Length, raw.Length > 4 ? raw[4].ToString() : "?");

                    RecievePacket p = new RecievePacket(raw, raw.Length);

                    if (m_socket.isDisconnected()) { return; }
                    p.SetPtr();
                    var b = p.Unpack8();
                    Network.ActionCodes.AC ac = Network.ActionCodes.AC.GetAction(b);
                    if (ac != null)
                    {
                        var c = this;
                        ac.ProcessPkt(c, p);
                    }


                    base.ProcessSocket(this, p);
                    m_inv.ProcessSocket(p);
                    if (m_settings != null)
                        m_settings.ProcessSocket(p);
                    if (m_battle != null)
                        m_battle.ProcessSocket(p);
                    if (m_tent != null)
                        m_tent.Process(this, p);


                }
                catch (Exception f)
                {
                    DebugSystem.Write("ProcessSocket Error: " + f.Message);
                    DebugSystem.Write("StackTrace: " + f.StackTrace);
                    m_socket.Disconnect();
                }
            }

            public void Disconnect()
            {
                if (!isDisconnected())
                    m_socket.Disconnect();
            }

            //public void Send(SendPacket pkt, bool queue = false)//TODO finished for multi packs
            //{
            //    if (!killFlag)
            //    {
            //        SendPacket o = pkt;
            //        if (QueuePkt != null)
            //            QueuePkt.PackArray(pkt.Data.ToArray());
            //        else if (queue)
            //            DatatoSend.Enqueue(pkt);
            //        else
            //        {
            //            switch (DataOut)
            //            {
            //                case SendType.Multi: MultiPkt.PackArray(o.Data.ToArray()); break;
            //                case SendType.Normal:
            //                    {
            //                        DLogger.NetworkLog(UserName, pkt.Data.ToArray());
            //                        killFlag = pkt.DisconnectAfter();
            //                        var data = pkt.Data.ToArray();
            //                        int offset = 0;
            //                        Encode(ref data);
            //                        int nret = 0;
            //                    retry:
            //                        if (nret < data.Length)
            //                        {
            //                            nret = (UInt16)socket.Send(data.Skip(offset).ToArray(), SocketFlags.None);
            //                            offset += nret; goto retry;
            //                        }
            //                    } break;
            //            }
            //        }
            //    }
            //}
                       

            #endregion
            
            #region Game.Battle
            public void OnBattle_Start(Game.Battle.BattleScene battle)
            {
            }

            void Battle_OnNewRound(List<Game.Battle.Fighter> fighters_on_my_side, List<Game.Battle.Fighter> fighters_on_other_side)
            {
                PacketBuilder tmp = new PacketBuilder();
                tmp.Begin(null);

                foreach (var f in fighters_on_my_side)
                {
                    tmp.Add(Tools.FromFormat("bbbbbwd", 51, 1, f.GridX, f.GridY, 25, f.CurHP, 0));
                    tmp.Add(Tools.FromFormat("bbbbbwd", 51, 1, f.GridX, f.GridY, 26, f.CurSP, 0));
                }
                foreach (var f in fighters_on_other_side)
                    tmp.Add(Tools.FromFormat("bbbbbwd", 51, 1, f.GridX, f.GridY, 25, f.CurHP, 0));

                tmp.Add(Tools.FromFormat("bb", 52, 1));
                Send(tmp.End());
            }

            #endregion
            
            #region Game.Mail

            #endregion

            #region Player
            //public void onPlayerLogin(uint id)
            //{
            //    if (Friends.Exists(c => c.ID == id))
            //    {
            //        SendPacket p = new SendPacket();
            //        p.PackArray(new byte[] { 14, 9 });
            //        p.Pack32(id);
            //        p.Pack8(0);
            //        Send(p);
            //    }
            //    for (int a = 0; a < MailBox.Count; a++)
            //    {
            //        if (MailBox[a].targetid == id && MailBox[a].type == "Send" && MailBox[a].isSent)
            //        {
            //            MailBox[a].isSent = true;
            //            SendMailTo(MailBox[a].targetid, MailBox[a].message);
            //        }
            //    }

            //}
            //public bool ContinueInteraction()
            //{
            //    if (DatatoSend.Count == 1)
            //    {
            //        var f = DatatoSend.Dequeue();
            //        Send(f);
            //        return (obj_interacting != null);
            //    }
            //    else if (DatatoSend.Count > 1)
            //    {
            //        Send(DatatoSend.Dequeue()); return true;
            //    }
            //    return false;
            //}
            //public void Send_3_Me()
            //{
            //    SendPacket p = new SendPacket();
            //    p.Pack8(3);
            //    p.Pack32(ID);
            //    p.Pack8((byte)Eqs.Body);
            //    p.Pack16(LoginMap);
            //    p.Pack16(X);
            //    p.Pack16(Y);
            //    p.Pack8(0); p.Pack8(Eqs.Head); p.Pack8(0);
            //    p.Pack16(HairColor);
            //    p.Pack16(SkinColor);
            //    p.Pack16(ClothingColor);
            //    p.Pack16(EyeColor);
            //    p.Pack8(Eqs.WornCount);//clothesAmmt); // ammt of clothes
            //    p.PackArray(Eqs.Worn_Equips);
            //    p.Pack32(0);
            //    p.PackString(CharacterName);
            //    p.PackString(Nickname);
            //    p.Pack32(0);
            //    Send(p);
            //}
            //public SendPacket _3Data()
            //{
            //    SendPacket p = new SendPacket();
            //    p.Pack8(3);
            //    p.Pack32(ID);
            //    p.Pack8((byte)Eqs.Body);
            //    p.Pack8((byte)Eqs.Element);
            //    p.Pack8((byte)Eqs.Level);
            //    p.Pack16(CurrentMap.MapID);
            //    p.Pack16(X);
            //    p.Pack16(Y);
            //    p.Pack8(0); p.Pack8(Eqs.Head); p.Pack8(0);
            //    p.Pack16(HairColor);
            //    p.Pack16(SkinColor);
            //    p.Pack16(ClothingColor);
            //    p.Pack16(EyeColor);
            //    p.Pack8(Eqs.WornCount);//clothesAmmt); // ammt of clothes
            //    p.PackArray(Eqs.Worn_Equips);
            //    p.Pack32(0); p.Pack8(0);
            //    p.PackBoolean(Eqs.Reborn);
            //    p.Pack8((byte)Eqs.Job);
            //    p.PackString(CharacterName);
            //    p.PackString(Nickname);
            //    p.Pack8(255);
            //    return p;
            //}
            //public void Send_5_3() //logging in player info
            //{
            //    SendPacket p = new SendPacket();
            //    p.PackArray(new byte[] { 5, 3 });
            //    p.Pack8((byte)Eqs.Element);
            //    p.Pack32((uint)CurHP);
            //    p.Pack16((ushort)CurSP);
            //    p.Pack16(Eqs.Str); //base str
            //    p.Pack16(Eqs.Con); //base con
            //    p.Pack16(Eqs.Int); //base int
            //    p.Pack16(Eqs.Wis); //base wis
            //    p.Pack16(Eqs.Agi); //base agi
            //    p.Pack8((byte)Eqs.Level); //lvl
            //    p.Pack64((ulong)Eqs.TotalExp); //exp ???
            //    p.Pack32((uint)Eqs.FullHP); //max hp
            //    p.Pack16((ushort)Eqs.FullSP); //max sp

            //    //-------------- 7 DWords
            //    p.Pack32(0);
            //    p.Pack32(0);
            //    p.Pack32(0);
            //    p.Pack32(0);
            //    p.Pack32(0);
            //    p.Pack32(0);
            //    p.Pack32(0);

            //    //--------------- Skills
            //    p.Pack16(0/*(ushort)MySkills.Count*/);
            //    //if (MySkills.Count > 0)
            //    //    p.PackArray(MySkills.GetSkillData());
            //    //p.Pack16(1); //ammt of skills
            //    //p.Pack16(188); p.Pack16(1); p.Pack16(0); p.Pack8(0); //skill data
            //    //--------------- table with rebirth and job
            //    p.Pack16(0); p.Pack16(0);
            //    p.Pack8(BitConverter.GetBytes(Eqs.Reborn)[0]); p.Pack8((byte)Eqs.Job); p.Pack8((byte)Eqs.Potential);

            //    Send(p);
            //}
            
            public bool Load_CharacterInfo(Character data)
            {
                if (data == null) return false;
                CharID = data.CharID;
                CharName = data.CharName;
                Slot = data.Slot;
                Head = data.Head;
                Body = data.Body;
                TotalExp = 95000478;//data.TotalEXP
                CharName = data.CharName;
                NickName = data.NickName;
                LoginMap = data.LoginMap;
                CurSP = data.CurSP;
                CurHP = data.CurHP;
                CurX = data.CurX;
                CurY = data.CurY;
                HairColor = data.HairColor;
                SkinColor = data.SkinColor;
                ClothingColor = data.ClothingColor;
                EyeColor = data.EyeColor;
                SetGold((int)data.Gold);
                Element = data.Element;
                Job = data.Job;
                Potential = data.Potential;
                foreach (var stat in data.GetStatArray())
                    SetBaseStat(stat[0], stat[1]);

                for (byte a = 1; a < 7; a++)
                    this[a].CopyFrom(data[a]);

                //remove
                FillHP();
                FillSP();

                return true;
            }
            public bool ContinueInteraction()
            {
                if (QueueData.Count == 1)
                {
                    m_socket.SendPacket(QueueData.Dequeue());
                    return false;// (object_interactingwith != null);
                }
                else if (QueueData.Count > 1)
                {
                    m_socket.SendPacket(QueueData.Dequeue()); return true;
                }
                return false;
            }

            #endregion

        //    #region Friends
        //    public void SendFriendList()
        //    {
        //        SendPacket y = new SendPacket();
        //        y.PackArray(new byte[] { 14, 5 });
        //        y.PackArray(new byte[]{100, 0, 0, 0, 6, 71, 77, 164, 164, 164, 223, 200, 0,      
        //0, 0, 0, 0, 28, 175, 125, 26, 28, 175, 125, 26, 0, 0});
        //        foreach (Character h in Friends)
        //        {
        //            y.Pack32(h.ID);
        //            y.PackString(h.CharacterName);
        //            y.Pack8((byte)h.Level);
        //            y.Pack8(BitConverter.GetBytes(h.Reborn)[0]);
        //            y.Pack8((byte)h.Job);
        //            y.Pack8((byte)h.Element);
        //            y.Pack8((byte)h.Body);
        //            y.Pack8(h.Head);
        //            y.Pack16(h.HairColor);
        //            y.Pack16(h.SkinColor);
        //            y.Pack16(h.ClothingColor);
        //            y.Pack16(h.EyeColor);
        //            y.PackString(h.Nickname);
        //            y.Pack8(0);
        //        }
        //        Send(y);
        //    }
        //    public void AddFriend(Player t)
        //    {
        //        if (Friends.Count == 50) return;
        //        if (!m_friends.Exists(c => c.ID == t.ID))
        //            m_friends.Add(t);
        //        SendPacket s = new SendPacket();
        //        s.PackArray(new byte[] { 14, 9 });
        //        s.Pack32(t.ID);
        //        s.Pack8(0);
        //        Send(s);
        //        s = new SendPacket();
        //        s.PackArray(new byte[] { 14, 7 });
        //        s.Pack32(t.ID);
        //        s.PackString("Test");
        //        Send(s);
        //    }
        //    public void DelFriend(uint t)
        //    {
        //        if (m_friends.Exists(c => c.ID == t))
        //            m_friends.Remove(m_friends.Single(c => c.ID == t));
        //        SendPacket s = new SendPacket();
        //        s.PackArray(new byte[] { 14, 4 });
        //        s.Pack32(t);
        //        Send(s);
        //    }
        //    public bool LoadFriends(string str)
        //    {
        //        foreach (string y in str.Split('&'))
        //        {
        //            if (y.Length > 0 && y != "none")
        //            {
        //                string[] f = y.Split(' ');
        //                m_friends.Add(myhost.CharDataBase.GetCharacterData(uint.Parse(f[0])));
        //            }
        //        }
        //        return true;
        //    }
        //    public string GetFriends_Flag
        //    {
        //        get
        //        {
        //            string query = "";
        //            for (int a = 0; a < m_friends.Count; a++)
        //            {
        //                query += m_friends[a].ID.ToString() + " " + m_friends[a].CharacterName;
        //                if (a < m_friends.Count)
        //                    query += "&";
        //            }
        //            if (query == "")
        //                query += "none";
        //            return query;
        //        }
        //    }
        //    #endregion

        //    #region Mail
        //    public void SendMailTo(Player t, string msg)
        //    {
        //        Mail a = new Mail();
        //        a.message = msg;
        //        a.id = ID;
        //        a.targetid = t.ID;
        //        a.type = "Send";
        //        a.isSent = true;
        //        MailBox.Add(a);
        //        t.RecvMailfrom(this, a.message);
        //    }
        //    public void SendMailTo(uint t, string msg)
        //    {
        //        Mail a = new Mail();
        //        a.message = msg;
        //        a.id = ID;
        //        a.targetid = t;
        //        a.type = "Send";
        //        a.isSent = false;
        //        MailBox.Add(a);
        //    }
        //    public override void RecvMailfrom(Player t, string msg, double Date = 0)
        //    {
        //        base.RecvMailfrom(t, msg, Date);
        //        SendPacket p = new SendPacket();
        //        p.PackArray(new byte[] { 14, 1 });
        //        p.Pack32(t.ID);
        //        p.PackArray(((Date == 0) ? BitConverter.GetBytes(DateTime.Now.ToOADate()) : BitConverter.GetBytes(Date)));
        //        for (int n = 0; n < msg.Length; n++)
        //            p.Pack8((byte)msg[n]);
        //        Send(p);
        //    }
        //    public string GetMailboxFlags()
        //    {
        //        string str = "none";
        //        if (MailBox.Count > 0)
        //        {
        //            for (int a = 0; a < MailBox.Count; a++)
        //            {
        //                str += MailBox[a].id + " " +
        //                    MailBox[a].targetid + " " +
        //                    MailBox[a].when + " " +
        //                    MailBox[a].message + " " +
        //                    MailBox[a].type + " " +
        //                    BitConverter.GetBytes(MailBox[a].isSent)[0].ToString() + " ";

        //                if (a < MailBox.Count)
        //                    str += "&";
        //            }
        //        }
        //        return str;
        //    }
        //    #endregion

        //    #region equips

        //    public bool WearEQ(byte index)
        //    {

        //        bool ret = false;
        //        InvItemCell i = new InvItemCell();

        //        i.CopyFrom(Inv[index]);
        //        if (i.ItemID > 0)
        //        {
        //            Inv[index].Clear();
        //            if (Eqs.Level >= i.Data.Level)
        //            {
        //                DataOut = SendType.Multi;
        //                var retrem = Eqs.SetEQ((byte)i.Data.EquipPos, i);
        //                if (retrem != null && retrem.ItemID > 0)
        //                    Inv.AddItem(retrem, index, false);
        //                Eqs.Send8_1();//send ac8
        //                SendPacket tmp = new SendPacket();
        //                tmp.PackArray(new byte[] { 5, 2 });
        //                tmp.Pack32(ID);
        //                tmp.Pack16(i.ItemID);
        //                CurrentMap.Broadcast(tmp, ID);
        //                tmp = new SendPacket();
        //                tmp.PackArray(new byte[] { 23, 17 });
        //                tmp.Pack8(index);
        //                tmp.Pack8(index);
        //                Send(tmp);
        //                ret = true;
        //                DataOut = SendType.Normal;

        //            }
        //            else
        //            {
        //            }
        //        }
        //        return ret;

        //    }

        //    public bool unWearEQ(byte src, byte dst)
        //    {
        //        bool ret = false;
        //        InvItemCell i = new InvItemCell();

        //        if (Eqs[src].ItemID > 0)
        //        {
        //            i.CopyFrom(Eqs[src]);//copy from clothes
        //            if (i != null && Inv.AddItem(i, dst, false) > 0)
        //            {
        //                Eqs.RemoveEQ(src);
        //                DataOut = SendType.Multi;
        //                SendPacket p = new SendPacket();
        //                p.PackArray(new byte[] { 23, 16 });
        //                p.Pack8(src);
        //                p.Pack8(dst);
        //                Send(p);
        //                Eqs.Send8_1();
        //                p = new SendPacket();
        //                p.PackArray(new byte[] { 5, 1 });
        //                p.Pack32(ID);
        //                p.Pack16(i.ItemID);
        //                CurrentMap.Broadcast(p, ID);
        //                ret = true;
        //                DataOut = SendType.Normal;
        //            }
        //            else if (i != null)
        //                Eqs.SetEQ(src, i);
        //        }
        //        return ret;

        //    }

        //    #endregion

        //    #region Team
        //    //public void KickMember(Player t)
        //    //{
        //    //    MemberLeave(t);
        //    //}
        //    //public void MakeLeader()
        //    //{
        //    //    SendPacket f = new SendPacket();
        //    //    f.Header(13, 15);
        //    //    f.Pack8(3);
        //    //    f.Pack32(own.CharacterTemplateID);
        //    //    foreach (Player u in myTeamMembers.ToArray())
        //    //        if (u.character.MyTeam.PartyLeader)
        //    //        {
        //    //            f.Pack32(u.CharacterTemplateID);
        //    //        }
        //    //    f.SetSize();
        //    //    leader = true;
        //    //    own.currentMap.Broadcast(f);

        //    //}
        //    //public void MemberLeave(Player l)
        //    //{
        //    //    if (l.character.MyTeam.leader)
        //    //        EndTeam();
        //    //    else if (myTeamMembers.Contains(l))
        //    //    {
        //    //        Rem(l);
        //    //    }
        //    //    Rem(l);
        //    //    l.character.MyTeam.Leave();
        //    //}
        //    //public void Leave()
        //    //{
        //    //    Send_5(53);
        //    //    Send_5(54);
        //    //    Send_5(183);
        //    //    SendPacket p = new SendPacket();
        //    //    p.Header(13, 4);
        //    //    p.Pack32(own.CharacterTemplateID);
        //    //    p.SetSize();
        //    //    own.currentMap.Broadcast(p);
        //    //    leader = false;
        //    //    myTeamMembers.Clear();
        //    //}
        //    //public void EndTeam()
        //    //{
        //    //    //end party
        //    //    foreach (Player s in myTeamMembers.ToArray())
        //    //        s.character.MyTeam.Leave();
        //    //}

        //    public void onPartyjoined(Player partyowner)
        //    {

        //    }
        //    #endregion
            #endregion

            #region Properties
            

            //public Inventory Inv { get { return m_inv; } }
            
           
            
            //public MailManager Mail { get { return m_Mail; } }
            //public Friendlist MyFriends { get { return m_friendlist; } }
            //public RiceBall Disguise { get { return m_riceball; } }
            //public PetList Pets { get { return m_petlist; } }
            //public Tent Tent { get { return m_tent; } }

            #endregion

            #region Internal Events
            void m_socket_onConnectionLost()
            {
                if (net != null && net.IsAlive) net.Abort();
                if (Disconnected != null) Disconnected(this);
            }
            public void onTick_Tick()
            {
                OnPropertyChanged("DisplayName");
            }
            #endregion

            #region Gui Update
            public string DisplayName { get { return SockAddress() + " ID: " + UserAcc.UserID + " User: " + UserAcc.UserName + " Char: " + CharName; } }

            #endregion
            
            #region Inotify Property
            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            #endregion
            
            

            


            
        }
    }
