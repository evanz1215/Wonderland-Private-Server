using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WloCharacterViewer.Models;
using WloCharacterViewer.Network;

namespace WloCharacterViewer.Automation
{
    public class AutoBattleEngine : INotifyPropertyChanged
    {
        private GameSession? _session;

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Notify();
                if (value && _session != null) CheckAndAct();
            }
        }

        private bool _autoEngage;
        public bool AutoEngage { get => _autoEngage; set { _autoEngage = value; Notify(); } }

        private ushort _skillId;
        public ushort SkillId { get => _skillId; set { _skillId = value; Notify(); } }

        private int _totalRounds;
        public int TotalRounds { get => _totalRounds; set { _totalRounds = value; Notify(); } }

        private int _totalBattles;
        public int TotalBattles { get => _totalBattles; set { _totalBattles = value; Notify(); } }

        public void Attach(GameSession session)
        {
            _session = session;
            session.Battle.PropertyChanged += OnBattleChanged;
        }

        public void Detach()
        {
            if (_session != null)
                _session.Battle.PropertyChanged -= OnBattleChanged;
            _session = null;
        }

        private void OnBattleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!Enabled || _session == null) return;
            if (e.PropertyName == nameof(BattleState.Phase))
                CheckAndAct();
        }

        private void CheckAndAct()
        {
            if (_session == null) return;
            var battle = _session.Battle;

            switch (battle.Phase)
            {
                case BattlePhase.WaitingForAction:
                    SendAttack();
                    break;

                case BattlePhase.Ended:
                case BattlePhase.None:
                    if (battle.Phase == BattlePhase.Ended)
                        TotalBattles++;

                    if (AutoEngage)
                    {
                        // Small delay before engaging next battle
                        Task.Delay(1500).ContinueWith(_ =>
                        {
                            if (Enabled && _session != null && !_session.Battle.InBattle)
                                EngageMonster();
                        });
                    }
                    break;
            }
        }

        private void SendAttack()
        {
            if (_session == null) return;

            // AC 50 sub 1: [srcX][srcY][dstX][dstY][skillId:16][unk1][unk2]
            // Default: attack from position (1,2) targeting (4,2), basic attack (skillId=0)
            var pw = new PacketWriter();
            pw.Pack8(50);
            pw.Pack8(1);
            pw.Pack8(1);  // srcX — player side
            pw.Pack8(2);  // srcY
            pw.Pack8(4);  // dstX — enemy side
            pw.Pack8(2);  // dstY — first enemy row
            pw.Pack16(SkillId); // 0 = basic attack
            pw.Pack8(0);
            pw.Pack8(0);
            _session.SendPacket(pw.Build());

            _session.Battle.Phase = BattlePhase.ActionSubmitted;
            TotalRounds++;
            _session.Log($"[AutoBattle] Attack sent (skill={SkillId}, round={TotalRounds})");
        }

        private void EngageMonster()
        {
            if (_session == null) return;

            // AC 11 sub 2: [pkType=3 NPC][targetID:32][clickID:16]
            // We need to know what NPC to engage — for now, use a placeholder
            // The user should be in a map with monsters; auto-engage will be refined
            _session.Log("[AutoBattle] Auto-engage not yet implemented (need NPC target)");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
