using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Battle;
using DataFiles;
using Network;


namespace Game.Code.PetRelated
{
    public class Pet : Game.Code.PetRelated.PetEquipManager
    {
        string name;

        public UInt32 OwnerID { get { return (owner != null) ? owner.CharID : 0; } set { } }
        public uint ID { get { return (npcData != null) ? npcData.NpcID : m_npcIDRaw; } }
        public byte Amity { get { return m_amity; } set { m_amity = value; } }
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(name)) return name;
                if (!string.IsNullOrEmpty(m_nameRaw)) return m_nameRaw;
                if (npcData != null) return ASCIIEncoding.ASCII.GetString(npcData.NpcName);
                return "";
            }
        }

        ushort m_npcIDRaw;
        string m_nameRaw;

        public Pet(byte mslot, PhoneixNpc src, Player owner)
            : base(new Action<SendPacket>(owner.Send))
        {
            Slot = mslot;
            this.owner = owner;
            npcData = src;
            m_amity = 60;

            base.Str = npcData.STR;
            base.Con = npcData.CON;
            base.Int = npcData.INT;
            base.Wis = npcData.WIS;
            base.Agi = npcData.AGI;
        }

        /// <summary>
        /// Construct a Pet from raw stat values (for captures/DB loads without PhoneixNpc data).
        /// </summary>
        public Pet(byte mslot, Player owner, ushort npcID, string name,
            ushort str, ushort con, ushort intl, ushort wis, ushort agi, byte element)
            : base(new Action<SendPacket>(owner.Send))
        {
            Slot = mslot;
            this.owner = owner;
            m_npcIDRaw = npcID;
            m_nameRaw = name;
            m_amity = 60;
            Element = (Affinity)element;

            base.Str = str;
            base.Con = con;
            base.Int = intl;
            base.Wis = wis;
            base.Agi = agi;
        }

        #region Fighter Properties
        public FighterState BattleState { get { return FighterState.Alive; } }
        public BattleRole BattlePosition { get; set; }
        public eFighterType TypeofFighter { get { return eFighterType.Pet; } }
        public BattleAction myAction { get; set; }
        public UInt16 ClickID { get { return 0; } set { } }
        public byte GridX { get; set; }
        public byte GridY { get; set; }
        public Int32 MaxHP { get { return FullHP; } }
        public Int16 MaxSP { get { return (short)FullSP; } }
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

        #endregion
    }


    public class PetList
    {
        readonly object mylock = new object();

        Player host;
        Dictionary<byte, Pet> petlist;
        Pet m_ridepet;
        Pet m_battlepet;

        public Pet BattlePet
        {
            get { return m_battlepet; }
            set
            {
                m_battlepet = value;
                if (value != null && host != null)
                    host.Send(Tools.FromFormat("bbd", 19, 1, value.ID));
            }
        }
        public Pet RidePet { get { return m_ridepet; } set { m_ridepet = value; } }
        public Pet this[byte key]
        {
            get
            {
                lock (mylock)
                {
                    if (petlist.ContainsKey(key))
                        return petlist[key];
                    return null;
                }
            }
            set
            {
                lock (mylock)
                {
                    if (petlist.ContainsKey(key))
                        petlist[key] = value;
                }
            }
        }

        public int Count { get { return petlist.Count; } }

        /// <summary>
        /// Get all pet slots and pets for DB persistence.
        /// </summary>
        public IEnumerable<KeyValuePair<byte, Pet>> AllPets
        {
            get { lock (mylock) { return petlist.ToList(); } }
        }

        public PetList(Player i)
        {
            host = i;
            petlist = new Dictionary<byte, Pet>(20);
        }

        /// <summary>
        /// Send pet stat data for all pets in slots 1-4
        /// </summary>
        public void SendPetlistStatData()
        {
            for (byte n = 1; n <= 4; n++)
            {
                if (petlist.ContainsKey(n) && petlist[n] != null)
                    petlist[n].Send8_1();
            }
        }

        /// <summary>
        /// Build AC 15,8 packet with pet list data
        /// </summary>
        public SendPacket GetPetlistData()
        {
            if (petlist.Count == 0)
                return null;

            SendPacket p = new SendPacket();
            p.Pack8(15);
            p.Pack8(8);

            for (byte n = 1; n <= 4; n++)
            {
                if (petlist.ContainsKey(n) && petlist[n] != null)
                {
                    var pet = petlist[n];
                    p.Pack8(n);
                    p.Pack16((ushort)pet.ID);
                    p.Pack32((uint)pet.TotalExp);
                    p.Pack8((byte)pet.Level);
                    p.Pack32((uint)pet.CurHP);
                    p.Pack16((ushort)pet.CurSP);
                    p.Pack16(pet.Int);
                    p.Pack16(pet.Str);
                    p.Pack16(pet.Con);
                    p.Pack16(pet.Agi);
                    p.Pack16(pet.Wis);
                    p.Pack8(0);
                    p.Pack8(pet.Amity);
                    p.Pack16(1);
                    p.Pack8(0);
                    // Skill data (placeholder — 3 skills x 2 bytes each)
                    p.Pack8(0); p.Pack8(0); // skill1 grade + exp
                    p.Pack8(0); p.Pack8(0); // skill2
                    p.Pack8(0); p.Pack8(0); // skill3
                    p.PackArray(pet.FullEqData);
                    p.Pack16(0);
                    p.Pack8(0); // reborn
                    p.Pack8(0); // potential
                }
            }

            return p;
        }

        /// <summary>
        /// Receive/add a new pet from NPC data.
        /// </summary>
        public void ReceivePet(PhoneixNpc npcData, bool load = false)
        {
            if (npcData.NpcID == 0) return;

            byte slot = 1;
            while (petlist.ContainsKey(slot) && slot < 20) { slot++; }
            if (slot >= 20) return;

            Pet pet = new Pet(slot, npcData, host);
            petlist.Add(slot, pet);

            if (!load)
            {
                // Notify client: AC 15,1 (new pet acquired)
                SendPacket pkt = new SendPacket();
                pkt.Pack8(15);
                pkt.Pack8(1);
                pkt.Pack32(host.CharID);
                pkt.Pack16((ushort)npcData.NpcID);
                pkt.Pack8((byte)pet.Level);
                pkt.Pack32((uint)pet.TotalExp);
                pkt.Pack8(0); pkt.Pack8(0); // skill1
                pkt.Pack8(0); pkt.Pack8(0); // skill2
                pkt.Pack8(0); pkt.Pack8(0); // skill3
                pkt.Pack8(pet.Amity);
                pkt.Pack16(0);
                pkt.Pack16(0);
                pkt.Pack8(0);
                host.Send(pkt);

                // Auto-set as battle pet
                if (BattlePet != null)
                    RestPet();
                BattlePet = pet;
            }
        }

        /// <summary>
        /// Receive/add a new pet from raw stat values (for battle capture).
        /// </summary>
        public void ReceivePetFromCapture(ushort npcID, string name,
            ushort str, ushort con, ushort intl, ushort wis, ushort agi, byte element,
            bool load = false)
        {
            if (npcID == 0) return;

            byte slot = 1;
            while (petlist.ContainsKey(slot) && slot < 20) { slot++; }
            if (slot >= 20) return;

            Pet pet = new Pet(slot, host, npcID, name, str, con, intl, wis, agi, element);
            petlist.Add(slot, pet);

            if (!load)
            {
                // Notify client: AC 15,1 (new pet acquired)
                SendPacket pkt = new SendPacket();
                pkt.Pack8(15);
                pkt.Pack8(1);
                pkt.Pack32(host.CharID);
                pkt.Pack16(npcID);
                pkt.Pack8((byte)pet.Level);
                pkt.Pack32((uint)pet.TotalExp);
                pkt.Pack8(0); pkt.Pack8(0); // skill1
                pkt.Pack8(0); pkt.Pack8(0); // skill2
                pkt.Pack8(0); pkt.Pack8(0); // skill3
                pkt.Pack8(pet.Amity);
                pkt.Pack16(0);
                pkt.Pack16(0);
                pkt.Pack8(0);
                host.Send(pkt);

                // Auto-set as battle pet
                if (BattlePet != null)
                    RestPet();
                BattlePet = pet;
            }
        }

        /// <summary>
        /// Dismiss/release a pet from the specified slot.
        /// </summary>
        public void DismissPet(byte slot)
        {
            if (!petlist.ContainsKey(slot)) return;

            if (BattlePet == petlist[slot])
                RestPet();

            petlist.Remove(slot);

            SendPacket pkt = new SendPacket();
            pkt.Pack8(15);
            pkt.Pack8(2);
            pkt.Pack32(host.CharID);
            pkt.Pack8(slot);
            host.Send(pkt);
        }

        /// <summary>
        /// Bring a pet into battle mode (visible on map, participates in fights).
        /// Broadcasts AC 15,4 to map.
        /// </summary>
        public void BringIntoBattle(byte slot)
        {
            if (!petlist.ContainsKey(slot)) return;

            if (BattlePet != null)
                RestPet();

            BattlePet = petlist[slot];

            if (host.CurMap != null)
            {
                SendPacket pkt = new SendPacket();
                pkt.Pack8(15);
                pkt.Pack8(4);
                pkt.Pack32(host.CharID);
                pkt.Pack16((ushort)BattlePet.ID);
                pkt.Pack8(0);
                pkt.Pack8(1);
                pkt.PackStringN(BattlePet.Name);
                pkt.Pack16(0); // weapon
                host.CurMap.Broadcast(pkt);
            }
        }

        /// <summary>
        /// Rest the current battle pet (remove from battle).
        /// Sends AC 19,2 to owner and AC 19,7 broadcast to map.
        /// </summary>
        public void RestPet()
        {
            if (BattlePet == null) return;

            host.Send(Tools.FromFormat("bb", 19, 2));

            if (host.CurMap != null)
            {
                SendPacket pkt = new SendPacket();
                pkt.Pack8(19);
                pkt.Pack8(7);
                pkt.Pack32(host.CharID);
                host.CurMap.Broadcast(pkt, "Ex", host.CharID);
            }

            m_battlepet = null; // bypass setter to avoid re-sending AC 19,1
        }

        /// <summary>
        /// Mount/ride a pet. Broadcasts AC 15,16 to map.
        /// </summary>
        public void RidePetAction(byte slot)
        {
            if (!petlist.ContainsKey(slot)) return;

            // If the pet is the current battle pet, remove from battle first
            if (BattlePet == petlist[slot])
            {
                if (host.CurMap != null)
                {
                    SendPacket dismiss = new SendPacket();
                    dismiss.Pack8(19);
                    dismiss.Pack8(7);
                    dismiss.Pack32(host.CharID);
                    host.CurMap.Broadcast(dismiss, "Ex", host.CharID);
                }
                RidePet = BattlePet;
                m_battlepet = null;
            }
            else
            {
                RidePet = petlist[slot];
            }

            SendPacket pkt = new SendPacket();
            pkt.Pack8(15);
            pkt.Pack8(16);
            pkt.Pack8(slot);
            pkt.Pack32(host.CharID);
            pkt.Pack16((ushort)petlist[slot].ID);
            host.CurMap.Broadcast(pkt);
            host.Send8_1(false);
        }

        /// <summary>
        /// Unmount/unride the current riding pet. Broadcasts AC 15,17.
        /// </summary>
        public void UnridePet()
        {
            if (RidePet == null) return;

            SendPacket pkt = new SendPacket();
            pkt.Pack8(15);
            pkt.Pack8(17);
            pkt.Pack32(host.CharID);

            if (host.CurMap != null)
                host.CurMap.Broadcast(pkt);

            host.Send8_1(false);
            RidePet = null;
        }
    }
}
