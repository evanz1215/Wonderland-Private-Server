using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;
using Game;
using Game.Code;


namespace Game.Battle
{
    /*
     *
     * 50,1 s 50,1(player x,y,enermy x,y,skill ushort, timer,0
( (flag:gb)  50.1 s (player x, y, enermy x, y, ushort skill, timer 0 )
     *
     * */
    public class BattleAction
    {
        public Fighter src;
        public Fighter dst;
        public BattleSkill skill;
        public byte unknownbyte;
        public byte unknownbyte2;
    }

    public class Battle
    {
        readonly object mylock = new object();
        bool blockupdt;

        #region Definitions
        DateTime roundend_time;

        public Dictionary<byte, BattleScene> Side;

        public eBattleType TypeofBattle;
        public eBattleState BattleState;
        public eBattleRoundState RoundState;

        public UInt16 Background,battleID;
        public Fighter startedby;

        #endregion
        public UInt16 BattleID { get { return battleID; } }
        public BattleScene this[BattleRole key]
        {
            get
            {
                return Side[(byte)key];
            }
        }


        public Battle(UInt16 BG,int BattleID)
        {
            Background = BG;
            Side = new Dictionary<byte, BattleScene>();
            Side.Add(2, new BattleScene((BattleRole)2, this));
            Side.Add(4, new BattleScene((BattleRole)4, this));
            Side.Add(5, new BattleScene((BattleRole)5, this));
        }

        #region BattleCmd Order

        public bool HasOrders
        {
            get
            {
                return (Side[2].FightersAlive.Count(c => c.myAction != null) > 0)
                    || (Side[5].FightersAlive.Count(c => c.myAction != null) > 0);
            }
        }

        public List<BattleAction> NextAction
        {
            get
            {
                List<Fighter> tmp = new List<Fighter>();
                List<BattleAction> cmdtmp = new List<BattleAction>();
                List<Fighter> fighters = new List<Fighter>();
                fighters.AddRange(Side[2].FightersAlive);
                fighters.AddRange(Side[5].FightersAlive);
                var fighters_with_cmds = fighters.Where(c => c.myAction != null).ToList();
                if (fighters_with_cmds.Count == 0) return cmdtmp;

                var orderedlist = fighters_with_cmds.OrderByDescending(c => c.FullSpd);
                var trgt = orderedlist.First();
                fighters_with_cmds.Remove(trgt);
                tmp.Add(trgt);

                // check for combo attacks (same side, similar speed, same target)
                for (int a = fighters_with_cmds.Count - 1; a >= 0; a--)
                {
                    orderedlist = fighters_with_cmds.OrderByDescending(c => c.FullSpd);
                    var chk = orderedlist.First();
                    fighters_with_cmds.Remove(chk);
                    if (trgt.BattlePosition == chk.BattlePosition && inSpdRange(chk, tmp) && trgt.myAction.dst == chk.myAction.dst)
                    {
                        tmp.Add(chk);
                    }
                    else
                    {
                        fighters_with_cmds.Add(chk);
                        break;
                    }
                }
                foreach (Fighter y in tmp)
                {
                    BattleAction tmpe = y.myAction;
                    y.myAction = null;
                    cmdtmp.Add(tmpe);
                }
                return cmdtmp;
            }
        }

        #endregion

        public int FighterCnt
        {
            get
            {
                return Side[2].Total_Fighters + Side[5].Total_Fighters;
            }
        }

        public IEnumerable<IReadOnlyList<Fighter>> AllFighters
        {
            get
            {
                return (from list in (from cell in Side.Values select cell.FighterList) select list);
            }
        }
        bool AllReady { get { return (Side[2].EveryoneReady && Side[5].EveryoneReady); } }

        #region Processing

        public void Process()
        {
            if (blockupdt) return;
            blockupdt = true;
            if (BattleState == eBattleState.Active)
            {
                // check if each side has players that are alive
                if (!(Side[2].Total_Fighters_Alive > 0 && Side[5].Total_Fighters_Alive > 0) && RoundState != eBattleRoundState.CalculatingState)
                {
                    BattleState = eBattleState.Ended;
                    EndBattle(eBattleLeaveType.BattleFinished); return;
                }
                // check if everyone sent a command during ready round
                if (AllReady && !HasOrders && RoundState == eBattleRoundState.ReadyState)
                    RoundState = eBattleRoundState.EndedState;
                else if (RoundState == eBattleRoundState.EndedState && HasOrders)
                    RoundState = eBattleRoundState.ReadyState;
                else if (RoundState == eBattleRoundState.EndedState && !HasOrders)
                    StartRound();
                else if (roundend_time < DateTime.Now && RoundState == eBattleRoundState.PrepState || AllReady && RoundState == eBattleRoundState.PrepState)
                    RoundState = eBattleRoundState.ReadyState;
                else if (AllReady && HasOrders && RoundState == eBattleRoundState.ReadyState)
                    Calculate();
            }
            blockupdt = false;
        }

        //Send StartRd info
        public void StartRound()
        {
            RoundState = eBattleRoundState.PrepState;
            roundend_time = DateTime.Now.AddSeconds(20);
            Side[2].OnNewRound();
            Side[5].OnNewRound();
        }

        //Rcv Attk
        public void PLayer_BattleAction(BattleAction data)
        {
            data.unknownbyte = 1;
            if (Side[(byte)BattleRole.Attacking].BattleActionRecieved(data) || Side[(byte)BattleRole.Defending].BattleActionRecieved(data))
            {
                SendPacket p = new SendPacket();
                p.PackArray(new byte[] { 53, 5 });
                p.Pack8(data.src.GridX);
                p.Pack8(data.src.GridY);

                foreach (Player gr in Side[(byte)BattleRole.Attacking].FighterList.Where(c => c is Player))
                    gr.Send(p);
                foreach (Player gr in Side[(byte)BattleRole.Defending].FighterList.Where(c => c is Player))
                    gr.Send(p);
            }
        }

        public void NPC_BattleAction(BattleAction data)
        {
            if (Side[(byte)BattleRole.Attacking].BattleActionRecieved(data) || Side[(byte)BattleRole.Defending].BattleActionRecieved(data))
            {
                SendPacket p = new SendPacket();
                p.PackArray(new byte[] { 53, 5 });
                p.Pack8(data.src.GridX);
                p.Pack8(data.src.GridY);

                foreach (Player gr in Side[(byte)BattleRole.Attacking].FighterList.Where(c => c is Player))
                    gr.Send(p);
                foreach (Player gr in Side[(byte)BattleRole.Defending].FighterList.Where(c => c is Player))
                    gr.Send(p);
            }
        }

        //End Battle for all players
        public void StartBattle()
        {
            foreach (Player fighter in Side[2].FighterList.Where(c => c is Player))
            {
                if (fighter != null && fighter.TypeofFighter == eFighterType.player)
                {
                    fighter.Send8_1();
                    Send_11_250(Background, Side[2].FighterList.ToList(), fighter);
                    Send_11_5(fighter);
                    SendPacket p = new SendPacket();
                    p.PackArray(new byte[] { 11, 10 });
                    p.Pack32(1);
                    fighter.Send(p);
                }
            }
            foreach (Player fighter in Side[5].FighterList.Where(c => c is Player))
            {
                if (fighter != null && fighter.TypeofFighter == eFighterType.player)
                {
                    fighter.Send8_1();
                    Send_11_250(Background, Side[5].FighterList.ToList(), fighter);
                    Send_11_5(fighter);
                    SendPacket p = new SendPacket();
                    p.PackArray(new byte[] { 11, 10 });
                    p.Pack32(1);
                    fighter.Send(p);
                }
            }
            BattleState = eBattleState.Active;
        }

        public void EndBattle(eBattleLeaveType t)
        {
            BattleState = eBattleState.Ended;
            // Notify all fighters on all sides
            foreach (var side in Side.Values)
            {
                foreach (Fighter f in side.FighterList.ToList())
                {
                    side.OnFighterLeft(this, f.BattlePosition, t, f);
                }
            }
        }

        #endregion



        #region Battle processing

        void Calculate()
        {
            RoundState = eBattleRoundState.CalculatingState;
            ushort skillid = 0;
            bool flee = false;
            byte dmtype = 0; //miss = 0, hpdmg = 25, spdmg = 26, debuff = 223, sealing = 221, healing = 232, buff = 225
            uint[] dmg = new uint[2];
            bool miss = false;
            // get list for next move which is already reordered by speed
            var e = NextAction;
            SendPacket moveData = new SendPacket(new byte[0]);
            List<Fighter> ppl_involved = new List<Fighter>();

            foreach (var q in e)
            {
                skillid = q.skill.SkillID;
                miss = false;
                dmg = new uint[2];

                var pat = q.skill.PatternofAttack();
                if (q.skill.EffectLayer == EffectLayer.Flee)
                    pat.Set(1, 1, 1);

                var re = AttackableTargets(
                    pat.GetTargets(new byte[] { q.dst.GridX, q.dst.GridY }, q.dst.BattlePosition),
                    q.dst.BattlePosition);

                if (re.Count >= 1)
                {
                    if (re.Count > 1)
                        moveData.Pack8(28);
                    else
                    {
                        moveData.Pack8((byte)Atktype(q.skill.EffectLayer));
                        moveData.Pack8(q.src.GridX);
                        moveData.Pack8(q.src.GridY);
                        moveData.Pack16(skillid);
                        moveData.Pack8(0); // affected by poison? switch to 1
                        moveData.Pack8((byte)re.Count);
                    }

                    foreach (var y in re)
                    {
                        if (SucessRate(q))
                        {
                            dmg = GetDamage(q, y);
                            switch (q.skill.EffectLayer)
                            {
                                case EffectLayer.Physical:
                                case EffectLayer.Magical:
                                    // Apply damage
                                    y.CurHP -= (int)dmg[0];
                                    if (y.CurHP < 0) y.CurHP = 0;
                                    break;
                                case EffectLayer.Healing1:
                                    // Heal target
                                    y.CurHP += (int)dmg[0];
                                    if (y.CurHP > y.MaxHP) y.CurHP = y.MaxHP;
                                    break;
                                case EffectLayer.Mana:
                                    // Restore SP
                                    y.CurSP += (int)dmg[0];
                                    if (y.CurSP > y.MaxSP) y.CurSP = y.MaxSP;
                                    break;
                                case EffectLayer.Revival:
                                    // Revive dead target with partial HP
                                    if (y.BattleState == FighterState.Dead)
                                    {
                                        y.CurHP = y.MaxHP / 4;
                                        dmg[0] = (uint)y.CurHP;
                                    }
                                    break;
                                case EffectLayer.Defend:
                                    // Set defend effect on self
                                    q.src.SkillEffect = BattleSkill.DefendAction();
                                    break;
                                case EffectLayer.Capture:
                                    // Attempt to capture mob as pet
                                    if (y is MobFighter && q.src is Player)
                                    {
                                        var capMob = (MobFighter)y;
                                        var capPlayer = (Player)q.src;
                                        if (capMob.Catchable && capPlayer.Pets.Count < 20)
                                        {
                                            capPlayer.Pets.ReceivePetFromCapture(
                                                capMob.NpcID, capMob.Name,
                                                (ushort)capMob.BaseStr, (ushort)capMob.BaseCon,
                                                (ushort)capMob.BaseInt, (ushort)capMob.BaseWis,
                                                (ushort)capMob.BaseAgi, (byte)capMob.Element);
                                            y.CurHP = 0; // remove mob from battle
                                            dmg[0] = 1;
                                        }
                                    }
                                    break;
                                case EffectLayer.Flee:
                                    flee = true;
                                    break;
                                case EffectLayer.Buffs1:
                                    // Apply buff to ally
                                    y.SkillEffect = q.skill;
                                    break;
                                case EffectLayer.Debuffs1:
                                case EffectLayer.Seals1:
                                case EffectLayer.Seals2:
                                    // Apply debuff/seal to enemy
                                    y.SkillEffect = q.skill;
                                    break;
                            }
                        }
                        else
                        {
                            switch (q.skill.EffectLayer)
                            {
                                case EffectLayer.Physical:
                                case EffectLayer.Magical: { miss = true; } break;
                                case EffectLayer.Flee: { miss = true; skillid = 60046; } break;
                                case EffectLayer.Seals1:
                                case EffectLayer.Seals2:
                                case EffectLayer.Debuffs1:
                                case EffectLayer.Capture: { miss = true; } break;
                            }
                        }
                        var et = GetState(miss, q.skill, y);

                        moveData.Pack8(y.GridX);
                        moveData.Pack8(y.GridY);
                        moveData.Pack8(et[0]);
                        moveData.Pack8(et[1]);
                        moveData.Pack8(et[2]);
                        moveData.Pack8(et[3]); //miss
                        moveData.Pack32(dmg[0]);
                        moveData.Pack16((ushort)dmg[1]);
                    }

                    ppl_involved.Add(q.src);
                }
            }
            if (flee && !miss)
                foreach (var s in ppl_involved)
                    RemFighter(eBattleLeaveType.RunAway, s);
            if (ppl_involved.Count > 0)
            {
                Side[(byte)BattleRole.Defending].Send_Attack(ppl_involved, moveData.Buffer.ToArray());
                Side[(byte)BattleRole.Attacking].Send_Attack(ppl_involved, moveData.Buffer.ToArray());
                Side[(byte)BattleRole.Watching].Send_Attack(ppl_involved, moveData.Buffer.ToArray());
            }
            RoundState = eBattleRoundState.EndedState;
        }

        #endregion

        #region BattleSide Control
        public void onJoinBattle(Fighter f)
        {
            Side[(byte)f.BattlePosition].AddFighter(TypeofBattle, f);
            if (f.TypeofFighter == eFighterType.player)
            {
                Send_11_250(Background, Side[(byte)f.BattlePosition].FighterList.ToList(), (Player)f);
                Send_11_5((Player)f);
                SendPacket p = new SendPacket();
                p.PackArray(new byte[] { 11, 10 });
                p.Pack32(1);
                ((Player)f).Send(p);
            }
        }
        //Watching Battle
        public void onWatchBattle(Fighter f)
        {
            f.BattlePosition = BattleRole.Watching;
            Side[4].AddFighter(TypeofBattle, f);
            Send_11_250(Background, Side[4].FighterList.ToList(), (Player)f);
            SendPacket p = new SendPacket();
            p.PackArray(new byte[] { 11, 10 });
            p.Pack32(1);
            ((Player)f).Send(p);
        }

        public void RemFighter(eBattleLeaveType o, Fighter src)
        {
            if (src == null) return;
            if (Side[(byte)BattleRole.Defending].FightersAlive.Count(c => c != src) == 0
                || Side[(byte)BattleRole.Attacking].FightersAlive.Count(c => c != src) == 0)
            {
                Side[(byte)src.BattlePosition].OnFighterLeft(this, src.BattlePosition, o, src);
                if (o == eBattleLeaveType.Dced)
                    EndBattle(eBattleLeaveType.RunAway);
                else if (o == eBattleLeaveType.Spawn)
                    EndBattle(eBattleLeaveType.BattleFinished);
                else
                    EndBattle(o);
            }
            else
                Side[(byte)src.BattlePosition].OnFighterLeft(this, src.BattlePosition, o, src);
        }
        public void RemFighter(eBattleLeaveType o, uint ID)
        {
            RemFighter(o, FindFighter(ID));
        }

        public Fighter FindFighter(uint ID)
        {
            foreach (var t in Side.Values.ToList())
                foreach (Fighter r in t.FighterList)
                    if (r.ID == ID)
                        return r;
            return null;
        }
        public Fighter FindFighter(byte x, byte y)
        {
            foreach (var t in Side.Values.ToList())
                foreach (Fighter r in t.FighterList)
                    if (r.GridX == x && r.GridY == y)
                        return r;
            return null;
        }
        #endregion

        #region Calculations
        double GetAtkDamage(ushort atk_matk, ushort SkillPower, ushort Def_mdef, float elementCorr)
        {
            var est = 0.0;
            if (atk_matk >= Def_mdef)
            {
                est = Math.Round(((atk_matk * (.5 + s_rng.NextDouble()) + SkillPower) - (Def_mdef * 0.98)) * elementCorr);
            }
            else
            {
                est = ((atk_matk * (.5 + s_rng.NextDouble()) + SkillPower) - ((int)Def_mdef * 0.98)) * elementCorr;
                if (est < 0) est = 1;
            }
            if (est < 0)
                return Def_mdef / 3;
            else return est;
        }
        double GetMatkDamage(int atk_matk, int SkillPower, int Def_mdef, float elementCorr)
        {
            var est = 0.0;
            if (atk_matk >= Def_mdef)
            {
                est = ((atk_matk * (1 + s_rng.NextDouble()) + SkillPower) - ((int)Def_mdef * 0.98)) * elementCorr;
            }
            else
            {
                est = ((atk_matk * (1.1 + s_rng.NextDouble()) + SkillPower) - ((int)Def_mdef * 1.3)) * elementCorr;
            }
            if (est < 0)
                return Def_mdef / 3;
            else return est;
        }
        double GetElementCorrection(Affinity hitter, Affinity target)
        {
            switch (hitter)
            {
                case Affinity.Fire:
                    {
                        switch (target)
                        {
                            case Affinity.Normal: return 1.0;
                            case Affinity.Fire: return 1.0;
                            case Affinity.Earth: return 1.0;
                            case Affinity.Water: return 0.6;
                            case Affinity.Wind: return 1.5;
                        }
                    } break;
                case Affinity.Earth:
                    {
                        switch (target)
                        {
                            case Affinity.Normal: return 1;
                            case Affinity.Fire: return 1.0;
                            case Affinity.Earth: return 1.0;
                            case Affinity.Water: return 1.7;
                            case Affinity.Wind: return 0.6;
                        }
                    } break;
                case Affinity.Water:
                    {
                        switch (target)
                        {
                            case Affinity.Normal: return 1;
                            case Affinity.Fire: return 1.7;
                            case Affinity.Earth: return 0.6;
                            case Affinity.Water: return 1.0;
                            case Affinity.Wind: return 1.0;
                        }
                    } break;
                case Affinity.Wind:
                    {
                        switch (target)
                        {
                            case Affinity.Normal: return 1;
                            case Affinity.Fire: return 0.4;
                            case Affinity.Earth: return 1.7;
                            case Affinity.Water: return 1.0;
                            case Affinity.Wind: return 1.0;
                        }
                    } break;
                case Affinity.Normal:
                    {
                        switch (target)
                        {
                            case Affinity.Normal: return 1;
                            case Affinity.Fire: return 1.3;
                            case Affinity.Earth: return 1.3;
                            case Affinity.Water: return 1.3;
                            case Affinity.Wind: return 1.3;
                        }
                    } break;
            }
            return 0;
        }
        bool CanFlee(byte srclvel, byte dstlevl)
        {
            if (srclvel < dstlevl)
            {
                int penalty = dstlevl / 6;
                return Succuss_miss(75 - penalty);
            }
            else
                return true;
        }
        static Random s_rng = new Random();

        /// <summary>
        /// Determine if an attack crits. Base 10% + (AGI/SPD bonus).
        /// </summary>
        bool ApplyCrit(Fighter src)
        {
            double chance = 10.0 + (src.FullSpd * 0.02);
            if (chance > 50) chance = 50; // cap at 50%
            return s_rng.Next(100) < chance;
        }
        bool Succuss_miss(double percentage)
        {
            return s_rng.Next(100) < percentage;
        }
        #endregion

        #region Packet Send Methods

        void Send_11_5(Player target)
        {
            List<Fighter> flist = new List<Fighter>();
            if (target != startedby)
            {
                flist.AddRange(Side[(byte)BattleRole.Attacking].FighterList.Where(h => h.TypeofFighter != eFighterType.player));
                flist.AddRange(Side[(byte)BattleRole.Defending].FighterList.Where(h => h.TypeofFighter != eFighterType.player));
            }
            else
            {
                flist.AddRange(Side[(byte)BattleRole.Attacking].FighterList.Where(h => h != startedby && h.BattlePosition != startedby.BattlePosition));
                flist.AddRange(Side[(byte)BattleRole.Defending].FighterList.Where(h => h != startedby && h.BattlePosition != startedby.BattlePosition));
            }

            if (flist.Count > 0)
                foreach (Fighter f in flist)
                {
                    SendPacket p = new SendPacket();
                    p.PackArray(new byte[] { 11, 5 });
                    p.Pack8((byte)f.BattlePosition);
                    p.Pack8((byte)f.TypeofFighter);
                    p.Pack32(f.ID);
                    p.Pack16(f.ClickID); p.Pack32(f.OwnerID);
                    p.Pack8(f.GridX); p.Pack8(f.GridY);
                    p.Pack32((uint)f.MaxHP); p.Pack16((ushort)f.MaxSP);
                    p.Pack32((uint)f.CurHP); p.Pack16((ushort)f.CurSP);
                    p.Pack8((byte)f.Level);
                    p.Pack8((byte)f.Element);
                    p.PackBool(f.Reborn); p.Pack8((byte)f.Job);
                    target.Send(p);
                }
        }

        public void Send_11_250(UInt16 background, List<Fighter> flist, Player target)
        {
            // sent to the player entering combat, listing all fighters
            if (flist.Count > 0)
            {
                SendPacket p = new SendPacket();
                p.PackArray(new byte[] { 11, 250 });
                p.Pack16(background);
                foreach (Fighter f in flist)
                {
                    p.Pack8((byte)f.BattlePosition);
                    p.Pack8((byte)f.TypeofFighter);
                    p.Pack32(f.ID);
                    p.Pack16(f.ClickID); p.Pack32(f.OwnerID);
                    p.Pack8(f.GridX); p.Pack8(f.GridY);
                    p.Pack32((uint)f.MaxHP); p.Pack16((ushort)f.MaxSP);
                    p.Pack32((uint)f.CurHP); p.Pack16((ushort)f.CurSP);
                    p.Pack8((byte)f.Level);
                    p.Pack8((byte)f.Element); p.PackBool(f.Reborn); p.Pack8((byte)f.Job);
                }
                target.Send(p);
            }
        }

        #endregion

        #region Target and Damage Helpers

        List<Fighter> AttackableTargets(List<byte[]> kl, BattleRole loc)
        {
            List<Fighter> tmp = new List<Fighter>();
            foreach (var k in kl)
            {
                var y = FindFighter(k[0], k[1]);
                if (y != null && y.BattlePosition == loc && y.BattleState != FighterState.Dead)
                    tmp.Add(y);
            }
            if (tmp.Count == 0)
                switch (loc)
                {
                    case BattleRole.Defending:
                        {
                            foreach (Fighter u in Side[(byte)BattleRole.Attacking].FightersAlive)
                            {
                                tmp.Add(u); break;
                            }
                        } break;
                    case BattleRole.Attacking:
                        {
                            foreach (Fighter u in Side[(byte)BattleRole.Defending].FightersAlive)
                            {
                                tmp.Add(u); break;
                            }
                        } break;
                }
            return tmp;
        }

        byte Atktype(EffectLayer q)
        {
            switch (q)
            {
                case EffectLayer.Flee:
                case EffectLayer.Buffs1:
                case EffectLayer.Defend:
                case EffectLayer.Healing1:
                case EffectLayer.Seals1:
                case EffectLayer.Magical:
                case EffectLayer.Capture:
                case EffectLayer.Physical: return 17;
                case EffectLayer.Mana: return 28;
                case EffectLayer.None: return 23;
            }
            return 0;
        }

        byte DmgType(EffectLayer q)
        {
            switch (q)
            {
                case EffectLayer.Flee:
                case EffectLayer.Defend:
                case EffectLayer.Magical:
                case EffectLayer.Physical: return 25; // HP damage
                case EffectLayer.Mana: return 26; // SP change
                case EffectLayer.Debuffs1: return 223; // debuff
                case EffectLayer.Seals1:
                case EffectLayer.Seals2: return 221; // seal
                case EffectLayer.Healing1:
                case EffectLayer.Revival: return 232; // heal
                case EffectLayer.Buffs1: return 225; // buff
            }
            return 0;
        }

        uint[] GetDamage(BattleAction d, Fighter trgt)
        {
            double tmp = 0;
            bool add = false;
            switch (d.skill.EffectLayer)
            {
                case EffectLayer.Physical:
                    {
                        add = true;
                        tmp = GetAtkDamage((ushort)d.src.FullAtk, d.skill.AttackPower(),
                            (ushort)trgt.FullDef, (float)GetElementCorrection(d.src.Element, trgt.Element));

                        // Defend reduces physical damage by 50%
                        if (trgt.SkillEffect != null && trgt.SkillEffect.EffectLayer == EffectLayer.Defend)
                            tmp *= 0.5;

                        // Crit: 10% base chance, doubles damage
                        if (ApplyCrit(d.src))
                            tmp *= 2.0;
                    }
                    break;
                case EffectLayer.Magical:
                    {
                        add = true;
                        tmp = GetMatkDamage(d.src.FullMatk, d.skill.AttackPower(),
                            trgt.FullMdef, (float)GetElementCorrection(d.src.Element, trgt.Element));

                        // Defend reduces magical damage by 30%
                        if (trgt.SkillEffect != null && trgt.SkillEffect.EffectLayer == EffectLayer.Defend)
                            tmp *= 0.7;

                        if (ApplyCrit(d.src))
                            tmp *= 1.5;
                    }
                    break;
                case EffectLayer.Healing1:
                    {
                        // Healing: restores HP to ally based on MATK + skill power
                        add = true;
                        tmp = d.src.FullMatk * 0.8 + d.skill.AttackPower() * 2.0;
                        if (tmp < 1) tmp = 1;
                    }
                    break;
                case EffectLayer.Mana:
                    {
                        // Mana restore: restores SP to ally
                        add = true;
                        tmp = d.src.FullMatk * 0.4 + d.skill.AttackPower();
                        if (tmp < 1) tmp = 1;
                    }
                    break;
            }

            if (tmp < 0) tmp = 1;
            return new uint[] { (uint)Math.Round(tmp), BitConverter.GetBytes(add)[0] };
        }

        byte[] GetState(bool miss, BattleSkill j, Fighter dst)
        {
            byte[] res = new byte[4];
            if (miss)
            {
                switch (j.EffectLayer)
                {
                    case EffectLayer.Physical:
                    case EffectLayer.Seals1:
                        {
                            res[0] = 0;
                            res[1] = 1;
                            res[2] = 1;
                            res[3] = 0;
                        } break;
                    case EffectLayer.Flee:
                        {
                            res[0] = 0;
                            res[1] = 2;
                            res[2] = 1;
                            res[3] = 0;
                        } break;
                }
            }
            else
            {
                if (dst.SkillEffect != null && dst.SkillEffect.EffectLayer == EffectLayer.Defend)
                {
                    if (j.SkillID == 11056)
                    {
                        res[0] = 1;
                        res[1] = 0;
                        res[2] = 1;
                        res[3] = (byte)DmgType(j.EffectLayer);
                    }
                    else
                    {
                        res[0] = 1;
                        res[1] = 1;
                        res[2] = 1;
                        res[3] = (byte)DmgType(j.EffectLayer);
                    }
                }
                else
                    switch (j.EffectLayer)
                    {
                        case EffectLayer.Flee:
                            {
                                res[0] = 0;
                                res[1] = 1;
                                res[2] = 1;
                                res[3] = (byte)DmgType(j.EffectLayer);
                            } break;
                        case EffectLayer.Physical:
                        case EffectLayer.Magical:
                        case EffectLayer.Healing1:
                            {
                                res[0] = 1;
                                res[1] = 0;
                                res[2] = 1;
                                res[3] = (byte)DmgType(j.EffectLayer);
                            } break;
                        case EffectLayer.Seals2:
                            {
                                if (j.UnknownByte7 == 0)
                                {
                                    res[0] = 1;
                                    res[1] = 0;
                                    res[2] = 1;
                                    res[3] = (byte)DmgType(j.EffectLayer);
                                }
                                else
                                {
                                    res[0] = 1;
                                    res[1] = 1;
                                    res[2] = 1;
                                    res[3] = (byte)DmgType(j.EffectLayer);
                                }
                            } break;
                    }
            }
            return res;
        }

        bool SucessRate(BattleAction t)
        {
            switch (t.skill.EffectLayer)
            {
                case EffectLayer.Flee:
                {
                    byte enemyLevel = 1;
                    switch (t.src.BattlePosition)
                    {
                        case BattleRole.Defending:
                            if (Side[(byte)BattleRole.Attacking].FightersAlive.Count > 0)
                                enemyLevel = Side[(byte)BattleRole.Attacking].FightersAlive[0].Level;
                            break;
                        case BattleRole.Attacking:
                            if (Side[(byte)BattleRole.Defending].FightersAlive.Count > 0)
                                enemyLevel = Side[(byte)BattleRole.Defending].FightersAlive[0].Level;
                            break;
                    }
                    return CanFlee(t.src.Level, enemyLevel);
                }
                case EffectLayer.Healing1:
                case EffectLayer.Revival:
                case EffectLayer.Mana:
                case EffectLayer.Defend:
                case EffectLayer.Buffs1:
                    return true; // Support skills always succeed

                case EffectLayer.Physical:
                {
                    // Physical hit: 85% base + (attacker SPD - target SPD) * 0.2, min 50%, max 98%
                    double hitChance = 85.0 + (t.src.FullSpd - t.dst.FullSpd) * 0.2;
                    if (hitChance < 50) hitChance = 50;
                    if (hitChance > 98) hitChance = 98;
                    return Succuss_miss(hitChance);
                }
                case EffectLayer.Magical:
                {
                    // Magical hit: 90% base, slightly harder to dodge
                    double hitChance = 90.0 + (t.src.FullSpd - t.dst.FullSpd) * 0.1;
                    if (hitChance < 60) hitChance = 60;
                    if (hitChance > 99) hitChance = 99;
                    return Succuss_miss(hitChance);
                }
                case EffectLayer.Seals1:
                case EffectLayer.Seals2:
                case EffectLayer.Debuffs1:
                {
                    // Status effects: 70% base
                    double hitChance = 70.0 + (t.src.Level - t.dst.Level) * 0.5;
                    if (hitChance < 30) hitChance = 30;
                    if (hitChance > 90) hitChance = 90;
                    return Succuss_miss(hitChance);
                }
                case EffectLayer.Capture:
                {
                    // Capture rate: based on target HP ratio. Lower HP = higher chance.
                    // Base 30% at full HP, up to 90% at 1 HP. Level difference penalty.
                    double hpRatio = (double)t.dst.CurHP / Math.Max(t.dst.MaxHP, 1);
                    double captureChance = 30.0 + (1.0 - hpRatio) * 60.0;
                    captureChance -= (t.dst.Level - t.src.Level) * 1.0;
                    if (captureChance < 5) captureChance = 5;
                    if (captureChance > 95) captureChance = 95;
                    return Succuss_miss(captureChance);
                }
                default:
                    return Succuss_miss(85);
            }
        }

        bool inSpdRange(Fighter chk, List<Fighter> agst)
        {
            bool ret = false;
            foreach (Fighter w in agst.Where(c => c != chk))
            {
                if ((w.FullSpd.CompareTo((UInt16)(chk.FullSpd - 100)) >= 0) &&
                        (chk.FullSpd.CompareTo((UInt16)(w.FullSpd - 100)) >= 0))
                    ret = true;
            }
            return ret;
        }

        #endregion
    }
}
