using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using System.Collections.Concurrent;
using DataFiles;
using Game.Maps;
using Network;

namespace Game.Code
{



    public partial class Tent : GameMap
    {
        Game.Player _owner;
        GameMap _ownerMap;
        TentFloor[] _floors;

        uint _mapx, _mapy;
        bool _locked, firstime, _closed;

        public Tent(Game.Player src)
        {
            _owner = src;
            firstime = true;
            _floors = new TentFloor[2];
            _floors[0] = new TentFloor();
            _floors[1] = new TentFloor();
            _closed = true;
        }

        /// <summary>
        /// Access tent floor by index (0 = 1F, 1 = 2F)
        /// </summary>
        public TentFloor GetFloor(byte floor)
        {
            if (floor > 1) return null;
            return _floors[floor];
        }

        public ushort Floor1Color { get { return _floors[0].FloorColor; } set { _floors[0].FloorColor = value; } }
        public ushort Floor1Wall { get { return _floors[0].Wallpaper; } set { _floors[0].Wallpaper = value; } }
        public ushort Floor2Color { get { return _floors[1].FloorColor; } set { _floors[1].FloorColor = value; } }
        public ushort Floor2Wall { get { return _floors[1].Wallpaper; } set { _floors[1].Wallpaper = value; } }

        public uint X { get { return _mapx; } }
        public uint Y { get { return _mapy; } }

        public void Open()
        {
            if (!_closed) return;
            _mapx = _owner.CurX;
            _mapy = _owner.CurY;
            _ownerMap = (GameMap)_owner.CurMap;
            _ownerMap.onTentOpened(this);
            _owner.Send(Tools.FromFormat("bbb", 62, 59, 2));
            _closed = false;
        }
        public void Close()
        {
            if (_closed) return;
            _ownerMap.onTentClosing(this);
            WarpData warp = new WarpData();
            warp.DstMap = (ushort)_ownerMap.MapID;
            warp.DstX_Axis = (ushort)_mapx;
            warp.DstY_Axis = (ushort)_mapy;
            //warp Players  out
            //foreach (var f in Floors)
            //{

            //    for (int a = 0; a < Players.Values.Count; a++)
            //    {
            //        Players.Values.ToList()[a].DataOut = SendType.Multi;
            //        SendPacket warpConf = new SendPacket();
            //        warpConf.PackArray(new byte[] { 20, 7 });
            //        SendPacket tmp = new SendPacket();
            //        tmp.PackArray(new byte[] { 23, 32 });
            //        tmp.Pack(Players.Values.ToList()[a].ID);
            //        Players.Values.ToList()[a].SendPacket(tmp);
            //        tmp = new SendPacket();
            //        tmp.PackArray(new byte[] { 23, 112 });
            //        tmp.Pack(p.ID);
            //        Players.Values.ToList()[a].SendPacket(tmp);
            //        tmp = new SendPacket();
            //        tmp.PackArray(new byte[] { 23, 132 });
            //        tmp.Pack(p.ID);
            //        Players.Values.ToList()[a].SendPacket(tmp);

            //        onWarp_Out(f.Key, ref Players.Values.ToList()[a], warp, false);// warp out of map
            //        p.X = warp.DstX_Axis;//switch x
            //        p.Y = warp.DstY_Axis;//switch y
            //        cGlobal.WLO_World.onTelePort(f.Key, warp, ref p);
            //        p.DataOut = SendType.Normal;

            //    }
            //}
            _closed = true;
        }

        #region Map Base
        public override uint MapID
        {
            get
            {
                return _owner.CharID;
            }

            set
            {
            }
        }
        public override string MapName
        {
            get
            {
                return _owner.CharName + "'s Home";
            }
        }
        public override MapType Type
        {
            get
            {
                return MapType.Tent;
            }
        }

        protected override void LoadData()
        {
            base.LoadData();
        }


        protected override void Warp_In(TeleportType teletype, Player src, WarpData from = null, byte portalID = 0)
        {
            base.Warp_In(teletype, src, from, portalID);
        }
        protected override void Warp_Out(byte portalID, Player src, WarpData To, bool toTent)
        {
            base.Warp_Out(portalID, src, To, toTent);
        }
        protected async override void SendMapInfo(Player t, bool login = false)
        {
            SendPacket p = new SendPacket();



            // Send placed items for each floor
            for (byte f = 0; f < 2; f++)
            {
                var itemPkt = _floors[f].GetItemListPacket(f);
                if (itemPkt != null)
                    t.Send(itemPkt);
            }

            t.Send(Tools.FromFormat("bbw", 62, 14, _floors[0].FloorColor));   // floor color
            t.Send(Tools.FromFormat("bbw", 62, 15, _floors[0].Wallpaper));    // wallpaper

            //65,11 ???

            p = new SendPacket();
            p.PackArray(new byte[] { 65, 7 });
            p.Pack16(0);
            t.Send(p);

            //extended Tent Item Info


            //ParkingGarage Info

            //p = new SendPacket();
            //p.PackArray(new byte[] { 23, 138 });
            //t.SendPacket(p);

            RCLibrary.Core.Networking.PacketBuilder tmp = new RCLibrary.Core.Networking.PacketBuilder();
            tmp.Begin(null);
            tmp.Add(Tools.FromFormat("bb", 23, 138));

            foreach (var r in m_playerlist)
            {
                tmp.Add(Tools.FromFormat("bbd", 23, 122, r.CharID));
                tmp.Add(Tools.FromFormat("bbdb", 10, 3, r.CharID, 255));

                if (r.CharID != t.CharID)
                {
                    r.Send(Tools.FromFormat("bbd", 23, 122, t.CharID));
                    r.Send(Tools.FromFormat("bbdb", 10, 3, t.CharID, 255));

                    if (r.Emote != 0)
                        tmp.Add(Tools.FromFormat("bbdb", 32, 2, r.CharID, r.Emote));

                    #region Pets in Map
                    //if (t.Pets.BattlePet != null)//to them
                    //{
                    //    SendPacket tmp = new SendPacket();
                    //    tmp.PackArray(new byte[] { 15, 4 });
                    //    tmp.Pack(t.CharID);
                    //    tmp.Pack(t.Pets.BattlePet.ID);
                    //    tmp.Pack((byte)0);
                    //    tmp.Pack((byte)1);
                    //    tmp.PackString(t.Pets.BattlePet.Name);
                    //    tmp.Pack16(0);//weapon
                    //    r.Send(tmp);
                    //}
                    //if (r.Pets.BattlePet != null)//to me
                    //{
                    //    SendPacket tmp = new SendPacket();
                    //    tmp.PackArray(new byte[] { 15, 4 });
                    //    tmp.Pack(r.CharID);
                    //    tmp.Pack(r.Pets.BattlePet.ID);
                    //    tmp.Pack((byte)0);
                    //    tmp.Pack((byte)1);
                    //    tmp.PackString(r.Pets.BattlePet.Name);
                    //    tmp.Pack16(0);//weapon
                    //    t.Send(tmp);
                    //}

                    #endregion
                    #region Riceball
                    //if (characters_in_map[a].riceBall.id > 0)
                    //{
                    //    if (characters_in_map[a].riceBall.active) g.ac5.Send_5(characters_in_map[a].riceBall.id, characters_in_map[a], t);
                    //}
                    //if (t.riceBall.id > 0)
                    //{
                    //    if (t.riceBall.active) g.ac5.Send_5(t.riceBall.id, t, characters_in_map[a]);
                    //}
                    #endregion
                    #region Team
                    //if (t.MyTeam.PartyLeader && t.MyTeam.hasParty && plist[a] != t)
                    //{
                    //    SendPacket fg = t.MyTeam._13_6;
                    //    plist[a].Send(fg);
                    //}
                    //if (plist[a].MyTeam.PartyLeader && plist[a].MyTeam.hasParty)
                    //{
                    //    SendPacket fg = plist[a].MyTeam._13_6;
                    //    t.Send(fg);
                    //}
                    #endregion
                    //if (Player.PlayerID != t.PlayerID)
                    //g.ac23.Send_74(Player.PlayerID, 0, c); //TODO find out what this does
                    #region Pets in Map
                    //AC 15,4 //possibly pet info for players on map with pets

                    //if (plist[a].CharacterState == PlayerState.inBattle)
                    //{
                    //    SendPacket qp = new SendPacket(t);
                    //    qp.PackArray(new byte[]{(11, 4);
                    //    qp.Pack((byte)2);
                    //    qp.Pack(plist[a].CharacterID);
                    //    qp.Pack16(0);
                    //    qp.Pack((byte)0);
                    //    qp.Send();
                    //}
                    #endregion
                    //23_76                    
                }
                tmp.Add(Tools.FromFormat("bbd", 23, 76, r.CharID));

            }

            tmp.Add(Tools.FromFormat("bb", 23, 102));
            tmp.Add(Tools.FromFormat("bb", 20, 8));
            t.Flags.Add(Game.PlayerFlag.InTent); //t.CharacterState = PlayerState.inMap;
        }

        #endregion

        void onPlayerLeaving(Game.Player src)
        {
        }

        #region Decoration

        /// <summary>
        /// Place an item from inventory into the tent.
        /// Removes the item from inventory and places it on the specified floor.
        /// </summary>
        public void PlaceItem(Player src, byte invSlot, byte floor, ushort x, ushort y, ushort z, byte rotation)
        {
            if (src.CharID != _owner.CharID) return;
            if (floor > 1) return;

            var item = src.Inv[invSlot];
            if (item == null || item.ItemID == 0) return;

            byte slot = _floors[floor].PlaceItem(item.ItemID, x, y, z, rotation);
            if (slot == 0)
            {
                // Floor full
                src.Send(Tools.FromFormat("bbb", 62, 20, 1));
                return;
            }

            src.Inv.RemoveItem(invSlot, 1);

            // Broadcast placement to all players in tent (AC 62,5)
            SendPacket pkt = new SendPacket();
            pkt.Pack8(62);
            pkt.Pack8(5);
            pkt.Pack8(floor);
            pkt.Pack8(slot);
            pkt.Pack16(item.ItemID);
            pkt.Pack16(x);
            pkt.Pack16(y);
            pkt.Pack16(z);
            pkt.Pack8(rotation);
            Broadcast(pkt);
        }

        /// <summary>
        /// Pick up a placed item and return it to inventory.
        /// </summary>
        public void PickupItem(Player src, byte floor, byte slot)
        {
            if (src.CharID != _owner.CharID) return;
            if (floor > 1) return;

            ushort itemID = _floors[floor].RemoveItem(slot);
            if (itemID == 0) return;

            src.Inv.AddItem(itemID, 1);

            // Broadcast removal (AC 62,6)
            SendPacket pkt = new SendPacket();
            pkt.Pack8(62);
            pkt.Pack8(6);
            pkt.Pack8(floor);
            pkt.Pack8(slot);
            Broadcast(pkt);
        }

        /// <summary>
        /// Move/rotate a placed item within the tent.
        /// </summary>
        public void MoveItem(Player src, byte floor, byte slot, ushort x, ushort y, byte rotation)
        {
            if (src.CharID != _owner.CharID) return;
            if (floor > 1) return;

            if (!_floors[floor].MoveItem(slot, x, y, rotation)) return;

            // Broadcast move (AC 62,7)
            SendPacket pkt = new SendPacket();
            pkt.Pack8(62);
            pkt.Pack8(7);
            pkt.Pack8(floor);
            pkt.Pack8(slot);
            pkt.Pack16(x);
            pkt.Pack16(y);
            pkt.Pack8(rotation);
            Broadcast(pkt);
        }

        /// <summary>
        /// Change floor color or wallpaper.
        /// </summary>
        public void SetFloorAppearance(Player src, byte floor, ushort floorColor, ushort wallpaper)
        {
            if (src.CharID != _owner.CharID) return;
            if (floor > 1) return;

            _floors[floor].FloorColor = floorColor;
            _floors[floor].Wallpaper = wallpaper;

            // Broadcast to all in tent
            Broadcast(Tools.FromFormat("bbw", 62, 14, floorColor));
            Broadcast(Tools.FromFormat("bbw", 62, 15, wallpaper));
        }

        #endregion

        public override void Process(Player src, RecievePacket data)
        {
            //resets pointer in packet
            data.SetPtr();



            base.Process(src, data);
        }
    }
    

    public class ItemBuild
    {
        //public delegate void TimerTick();
        //public event TimerTick TimerEventHandler;
        //public Item item;               
        public ushort CurTimer;
        public ushort TimerTotal;
        public byte qnt;
       // public bool End { get { return End; } }

       // System.Windows.Forms.Timer t;

        //public void start()
        //{
        //    t = new System.Windows.Forms.Timer();
        //    t.Interval = 15000; // specify interval time as you want
        //    t.Tick += new EventHandler(timer_Tick);

        //    t.Init();
        //}
        //public void timer_Tick(object sender, EventArgs e)
        //{
        //    t.Stop();
        //    if (TimerEventHandler != null)
        //        TimerEventHandler();
        //}
        
    }    
    
}
