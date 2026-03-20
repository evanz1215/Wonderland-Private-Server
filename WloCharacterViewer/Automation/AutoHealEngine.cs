using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WloCharacterViewer.Models;
using WloCharacterViewer.Network;

namespace WloCharacterViewer.Automation
{
    public class AutoHealEngine : INotifyPropertyChanged
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
                if (value && _session != null) TryHeal();
            }
        }

        private int _hpThreshold = 50;
        public int HPThreshold { get => _hpThreshold; set { _hpThreshold = value; Notify(); } }

        private int _spThreshold = 30;
        public int SPThreshold { get => _spThreshold; set { _spThreshold = value; Notify(); } }

        public void Attach(GameSession session)
        {
            _session = session;
            session.Character.PropertyChanged += OnCharacterChanged;
        }

        public void Detach()
        {
            if (_session != null)
                _session.Character.PropertyChanged -= OnCharacterChanged;
            _session = null;
        }

        private void OnCharacterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!Enabled || _session == null) return;
            if (e.PropertyName == nameof(CharacterState.CurHP) || e.PropertyName == nameof(CharacterState.CurSP))
                TryHeal();
        }

        private void TryHeal()
        {
            if (_session == null) return;
            var c = _session.Character;

            if (c.FullHP > 0 && c.HPPercent < HPThreshold)
            {
                // Find HP potion in bag and use it
                var potion = _session.Inventory.Bag.FirstOrDefault(i => IsHPPotion(i.ItemId));
                if (potion != null)
                {
                    SendUseItem(potion.Slot);
                    _session.Log($"[AutoHeal] Used HP potion (slot {potion.Slot})");
                }
            }

            if (c.FullSP > 0 && c.SPPercent < SPThreshold)
            {
                var potion = _session.Inventory.Bag.FirstOrDefault(i => IsSPPotion(i.ItemId));
                if (potion != null)
                {
                    SendUseItem(potion.Slot);
                    _session.Log($"[AutoHeal] Used SP potion (slot {potion.Slot})");
                }
            }
        }

        private void SendUseItem(byte slot)
        {
            // AC 23 sub 15 — use item at slot
            var pw = new PacketWriter();
            pw.Pack8(23);
            pw.Pack8(15);
            pw.Pack8(slot);
            _session!.SendPacket(pw.Build());
        }

        private static bool IsHPPotion(ushort itemId)
        {
            // Common HP potion IDs — extend as needed
            return itemId >= 30001 && itemId <= 30010;
        }

        private static bool IsSPPotion(ushort itemId)
        {
            return itemId >= 30011 && itemId <= 30020;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
