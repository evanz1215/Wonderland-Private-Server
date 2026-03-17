using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game.Battle
{

    public delegate void BattleRoundInfo(List<Game.Battle.Fighter> fighters_on_my_side, List<Game.Battle.Fighter> fighters_on_other_side);

    /// <summary>
    /// Object that will house information pertaining to battle
    ///
    /// this information is based on which side the player is on and who is on the same side
    /// </summary>
    public class BattleScene
    {
        readonly Battle _battleref;
        readonly BattleRole role;

        int round;

        List<Fighter> fighterlist; public IReadOnlyList<Fighter> FighterList { get { return fighterlist; } }


        public event BattleRoundInfo onRoundStart;
        public event EventHandler onBattleOver;

        public BattleScene(BattleRole sideRole, Battle owner)
        {
            _battleref = owner;
            role = sideRole;

            round = 0;
            fighterlist = new List<Fighter>();
        }

        public int Total_Fighters { get { return fighterlist.Count; } }
        public int Total_Fighters_Alive { get { return fighterlist.Count(c => c.CurHP > 0); } }

        public List<Fighter> FightersAlive
        {
            get
            {
                return fighterlist.Where(c => c.BattleState == FighterState.Alive).ToList();
            }
        }

        public List<Fighter> FightersBySpeed
        {
            get
            {
                return fighterlist.OrderBy(r => r.FullSpd).ToList();
            }
        }

        #region Methods
        /// <summary>
        /// Used to Poll if all fighters have finished what they need to do
        /// </summary>
        public bool EveryoneReady
        {
            get
            {
                if (fighterlist.Count(c => c.ActionDone) == fighterlist.Count)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Check if a grid position is available (no fighter present)
        /// </summary>
        bool AvailableSlot(bool IsPlayer, bool IsPet, bool IsMob, out byte[] location)
        {
            byte x, y;

            if (IsPlayer)
            {
                x = 0;
                y = 2;
                switch (role)
                {
                    case BattleRole.Defending: x = 1; break;
                    case BattleRole.Attacking: x = 4; break;
                }
                switch (fighterlist.Count)
                {
                    case 0: break;
                    case 1: y += 1; break;
                    case 2: y -= 1; break;
                    case 3: y += 2; break;
                }
                location = new byte[] { x, y };
                return !HasFighter(location);
            }
            else if (IsMob)
            {
                x = 0;
                y = 2;
                switch (role)
                {
                    case BattleRole.Defending: x = 1; break;
                    case BattleRole.Attacking: x = 4; break;
                }
                switch (fighterlist.Count)
                {
                    case 0: break;
                    case 1: y += 1; break;
                    case 2: y -= 1; break;
                    case 3: y += 2; break;
                }
                location = new byte[] { x, y };
                return !HasFighter(location);
            }

            location = new byte[0];
            return false;
        }

        /// <summary>
        /// Determines if a specific location has a fighter
        /// </summary>
        bool HasFighter(byte[] location)
        {
            foreach (Fighter f in fighterlist)
                if (f.GridX == location[0] && f.GridY == location[1])
                    return true;
            return false;
        }
        /// <summary>
        /// Determines if a specific location has a fighter
        /// </summary>
        bool HasFighter(byte[] location, out bool IsAlive)
        {
            IsAlive = false;
            foreach (Fighter f in fighterlist)
            {
                if (f.GridX == location[0] && f.GridY == location[1])
                {
                    IsAlive = f.CurHP > 0;
                    return true;
                }
            }
            return false;
        }


        public void OnNewRound()
        {
            round++;
            // Set round end time for all fighters so ActionDone doesn't trigger prematurely
            DateTime rdEnd = DateTime.Now.AddSeconds(20);
            foreach (Fighter f in fighterlist.ToList())
            {
                f.RdEndTime = rdEnd;
                f.myAction = null;

                if (f.SkillEffect != null)
                    f.SkillEffect.DecreaseTurns();

                if (f.TypeofFighter == eFighterType.Npc_Mob)
                {
                    // NPC AI: select target and create battle action
                    if (f is MobFighter)
                    {
                        BattleRole enemySide = (role == BattleRole.Attacking) ? BattleRole.Defending : BattleRole.Attacking;
                        var enemies = _battleref[enemySide].FightersAlive;
                        ((MobFighter)f).ProcessAI(enemies);
                    }
                }
                else if (f.TypeofFighter == eFighterType.player)
                {
                    var player = f as Player;
                    if (player == null) continue;

                    BattleRole rndptr = (role == BattleRole.Attacking) ? BattleRole.Defending : BattleRole.Attacking;

                    // Send HP/SP updates for all fighters on both sides
                    switch (role)
                    {
                        case BattleRole.Attacking:
                            {
                                foreach (Fighter ft in fighterlist.ToList())
                                {
                                    Send51_1(ft, 25, (ushort)ft.CurHP, player);
                                    Send51_1(ft, 26, (ushort)ft.CurSP, player);
                                }
                                foreach (Fighter lt in _battleref[BattleRole.Defending].FighterList)
                                    Send51_1(lt, 25, (ushort)lt.CurHP, player);
                            } break;
                        case BattleRole.Defending:
                            {
                                foreach (Fighter ft in fighterlist.ToList())
                                {
                                    Send51_1(ft, 25, (ushort)ft.CurHP, player);
                                    Send51_1(ft, 26, (ushort)ft.CurSP, player);
                                }
                                foreach (Fighter lt in _battleref[BattleRole.Attacking].FighterList)
                                    Send51_1(lt, 25, (ushort)lt.CurHP, player);
                            } break;
                    }
                    // Send round start signal (AC 52,1)
                    SendPacket p = new SendPacket();
                    p.PackArray(new byte[] { 52, 1 });
                    player.Send(p);
                }
            }

            if (onRoundStart != null)
            {
                BattleRole rndptr = (role == BattleRole.Attacking) ? BattleRole.Defending : BattleRole.Attacking;
                onRoundStart(fighterlist, _battleref[rndptr].fighterlist);
            }
        }


        bool OnNewFighter(Fighter src)
        {
            src.OnNewBattle(this);

            byte[] loc;

            if (src.TypeofFighter == eFighterType.player && AvailableSlot(true, false, false, out loc))
            {
                src.GridX = loc[0];
                src.GridY = loc[1];
                fighterlist.Add(src);
                return true;
            }
            else if (src.TypeofFighter == eFighterType.Npc_Mob && AvailableSlot(false, false, true, out loc))
            {
                src.GridX = loc[0];
                src.GridY = loc[1];
                fighterlist.Add(src);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Public method to add a fighter to this battle scene
        /// </summary>
        public void AddFighter(eBattleType battleType, Fighter fighter)
        {
            OnNewFighter(fighter);
        }

        public void OnFighterLeft(Battle src, BattleRole side, eBattleLeaveType exit, Fighter fighter)
        {
            if (this.role == side || fighter.BattlePosition == this.role)
            {
                bool defeat = false;
                if (fighter.TypeofFighter == eFighterType.Pet || fighter.TypeofFighter == eFighterType.Npc_Mob)
                {
                    SendPacket p = new SendPacket();
                    p.PackArray(new byte[] { 11, 1 });
                    p.Pack8((byte)fighter.GridX);
                    p.Pack8((byte)fighter.GridY);

                    foreach (var list in src.AllFighters)
                        foreach (Fighter u in list)
                            if (u is Player)
                                ((Player)u).Send(p);

                    if (fighter.TypeofFighter == eFighterType.Pet)
                    {
                        if (fighter.CurHP == 0)
                        {
                            defeat = true;
                            fighter.CurHP++;
                        }
                    }
                }
                else if (fighter.TypeofFighter == eFighterType.player)
                {
                    var player = fighter as Player;
                    if (player != null)
                    {
                        DebugSystem.Write(string.Format("[OnFighterLeft] Sending battle-end packets to player ID={0} exit={1}", player.ID, exit));

                        // Send battle end packet (AC 11,12)
                        SendPacket t = new SendPacket();
                        t.PackArray(new byte[] { 11, 12 });
                        if (fighter == src.startedby)
                            t.Pack8(1);
                        else
                            t.Pack8(2);
                        t.Pack8(0);
                        player.Send(t);

                        // Restore HP if alive
                        if (player.CurHP == 0)
                        {
                            defeat = true;
                            player.CurHP = 1;
                        }

                        // Process rewards on battle finish
                        if (exit == eBattleLeaveType.BattleFinished && !defeat)
                        {
                            DistributeRewards(src, player);
                        }
                        else if (defeat)
                        {
                            // Lose 6% of current EXP on defeat
                            int expLoss = (int)Math.Round(player.CurExp * 0.06);
                            if (player.CurExp - expLoss > 0)
                                player.CurExp -= expLoss;
                        }

                        // Send battle leave notification (AC 11,0)
                        SendPacket p = new SendPacket();
                        p.PackArray(new byte[] { 11, 0 });
                        p.Pack32(fighter.ID);
                        p.Pack32(0);
                        if (player.CurMap != null)
                            player.CurMap.Broadcast(p);

                        // Send fighter removed (AC 11,1)
                        p = new SendPacket();
                        p.PackArray(new byte[] { 11, 1 });
                        p.Pack8((byte)fighter.GridX);
                        p.Pack8((byte)fighter.GridY);
                        p.Pack8(0);
                        player.Send(p);

                        // Clear battle reference
                        player.MyBattle = null;
                    }
                }
                fighterlist.Remove(fighter);
            }
        }

        /// <summary>
        /// Receives and stores a battle action from a fighter
        /// </summary>
        public bool BattleActionRecieved(BattleAction h)
        {
            foreach (Fighter e in fighterlist.ToList())
                if (h.src == e)
                {
                    e.myAction = h;
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Send attack animation/result data to all players on this side
        /// </summary>
        public void Send_Attack(List<Fighter> attackers, byte[] data)
        {
            SendPacket w = new SendPacket();
            w.PackArray(new byte[] { 50, 1 });
            w.PackArray(data);

            // Debug: hex dump of raw data and full packet
            DebugSystem.Write(string.Format("[Send_Attack] side={0} attackers={1} rawDataLen={2} rawHex={3}",
                role, attackers.Count, data.Length,
                BitConverter.ToString(data.Length > 64 ? data.Take(64).ToArray() : data)));
            DebugSystem.Write(string.Format("[Send_Attack] fullPktLen={0} pktHex={1}",
                w.Buffer.Count(),
                BitConverter.ToString(w.Buffer.Count() > 80 ? w.Buffer.Take(80).ToArray() : w.Buffer.ToArray())));

            foreach (Fighter rt in fighterlist.ToList())
            {
                if (rt.TypeofFighter == eFighterType.player && rt is Player)
                {
                    var player = (Player)rt;
                    // Send attacker ready packets (AC 50,6)
                    foreach (var s in attackers)
                    {
                        SendPacket p = new SendPacket();
                        p.PackArray(new byte[] { 50, 6 });
                        p.Pack8((byte)s.GridX);
                        p.Pack8((byte)s.GridY);
                        p.Pack8(0);
                        player.Send(p);
                        DebugSystem.Write(string.Format("[Send_Attack] sent AC50,6 ready: grid=({0},{1}) to player {2}",
                            s.GridX, s.GridY, player.CharID));
                    }
                    // Send attack result data (AC 50,1)
                    player.Send(w);
                    DebugSystem.Write(string.Format("[Send_Attack] sent AC50,1 results to player {0}", player.CharID));
                }
            }
        }

        /// <summary>
        /// Send stat update to a player (AC 51,1)
        /// Used to sync HP/SP values at round start
        /// </summary>
        public void Send51_1(Fighter t, byte statType, ushort amount, Player target)
        {
            SendPacket p = new SendPacket();
            p.PackArray(new byte[] { 51, 1 });
            p.Pack8((byte)t.GridX);
            p.Pack8((byte)t.GridY);
            p.Pack8(statType);
            p.Pack16(amount);
            p.Pack8(0);
            target.Send(p);
        }

        #endregion

        /// <summary>
        /// Distribute EXP and Gold rewards to a player after battle victory.
        /// Collects rewards from all defeated MobFighters on the opposing side.
        /// Also shares EXP with team members on the same map (team bonus).
        /// </summary>
        void DistributeRewards(Battle battle, Player player)
        {
            uint totalExp = 0;
            uint totalGold = 0;

            // Collect rewards from all sides' dead mobs + track killed NPC IDs for quests
            List<ushort> killedNpcIDs = new List<ushort>();
            foreach (var sideKvp in battle.Side)
            {
                foreach (Fighter f in sideKvp.Value.FighterList)
                {
                    if (f.TypeofFighter == eFighterType.Npc_Mob && f.BattleState == FighterState.Dead && f is MobFighter)
                    {
                        var mob = (MobFighter)f;
                        totalExp += mob.ExpReward;
                        totalGold += mob.GoldReward;
                        killedNpcIDs.Add(mob.NpcID);
                    }
                }
            }

            // Update kill quest progress for each killed mob
            if (killedNpcIDs.Count > 0 && player.Quests != null)
            {
                foreach (var quest in player.Quests.ActiveQuests)
                {
                    if (Player.GetQuestTemplate != null)
                    {
                        var template = Player.GetQuestTemplate(quest.QID);
                        if (template != null && template.TargetNpcID > 0 && template.TargetItemID == 0)
                        {
                            int killCount = killedNpcIDs.Count(id => id == template.TargetNpcID);
                            if (killCount > 0)
                                player.Quests.UpdateProgress(quest.QID, killCount);
                        }
                    }
                }
            }

            // Count alive players on winning side to split rewards
            BattleRole playerSide = player.BattlePosition;
            int alivePlayerCount = 0;
            if (battle.Side.ContainsKey((byte)playerSide))
            {
                alivePlayerCount = battle.Side[(byte)playerSide].FighterList
                    .Count(f => f.TypeofFighter == eFighterType.player && f.BattleState == FighterState.Alive);
            }
            if (alivePlayerCount < 1) alivePlayerCount = 1;

            // Split rewards among alive players in battle
            uint expShare = totalExp / (uint)alivePlayerCount;
            uint goldShare = totalGold / (uint)alivePlayerCount;

            // Apply global EXP multiplier (e.g., double exp events)
            if (Player.GetExpMultiplier != null)
            {
                double mult = Player.GetExpMultiplier();
                if (mult > 1.0)
                    expShare = (uint)(expShare * mult);
            }

            // Team EXP sharing: team members on same map get 30% of exp
            if (player.hasParty && expShare > 0)
            {
                // Team bonus: 10% extra exp per party member (up to 40% bonus)
                int partySize = player.TeamMembers.Count;
                double teamBonus = 1.0 + Math.Min(partySize, 4) * 0.10;
                uint boostedExp = (uint)(expShare * teamBonus);

                // Award full boosted exp to battle participant
                player.CurExp = (int)boostedExp;

                // Award 30% share to team members on same map but not in this battle
                uint teamShare = (uint)(expShare * 0.30);
                if (teamShare > 0)
                {
                    foreach (var member in player.TeamMembers)
                    {
                        if (member.MyBattle != null) continue; // already in a battle
                        if (member.CurMap == null || player.CurMap == null) continue;
                        if (member.CurMap.MapID != player.CurMap.MapID) continue;

                        member.CurExp = (int)teamShare;
                    }
                }
            }
            else if (expShare > 0)
            {
                // Solo player — award exp directly
                player.CurExp = (int)expShare;
            }

            // Award pet EXP (battle pet gets 80% of player's share)
            if (expShare > 0 && player.Pets != null && player.Pets.BattlePet != null)
            {
                var pet = player.Pets.BattlePet;
                if (pet.CurHP > 0)
                {
                    uint petExp = (uint)(expShare * 0.80);
                    if (petExp > 0)
                        pet.CurExp = (int)petExp;
                }
            }

            // Award Gold
            if (goldShare > 0)
            {
                player.AddGold((int)goldShare);
                player.SendGold();
            }
        }

        #region Battle Proxy Methods
        /// <summary>
        /// Get the parent Battle reference
        /// </summary>
        public Battle BattleRef { get { return _battleref; } }

        /// <summary>
        /// Find a fighter by grid position (delegates to Battle)
        /// </summary>
        public Fighter FindFighter(byte x, byte y)
        {
            return _battleref.FindFighter(x, y);
        }

        /// <summary>
        /// Current round state (delegates to Battle)
        /// </summary>
        public eBattleRoundState RoundState { get { return _battleref.RoundState; } }
        #endregion

        public void ProcessSocket(RecievePacket g)
        {
        }

    }
}
