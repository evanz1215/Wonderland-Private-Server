using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace WloCharacterViewer.Models
{
    public enum BattlePhase
    {
        None,
        WaitingForAction,
        ActionSubmitted,
        RoundProcessing,
        Ended
    }

    public class BattleEntity : INotifyPropertyChanged
    {
        public byte Position { get; set; }
        public string Name { get; set; } = "";
        public ushort ModelId { get; set; }
        public uint CurHP { get; set; }
        public uint MaxHP { get; set; }
        public bool IsAlive => CurHP > 0;
        public bool IsEnemy { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BattleState : INotifyPropertyChanged
    {
        private static readonly object _alliesLock = new();
        private static readonly object _enemiesLock = new();

        private BattlePhase _phase = BattlePhase.None;
        public BattlePhase Phase { get => _phase; set { _phase = value; Notify(); Notify(nameof(InBattle)); } }

        public bool InBattle => Phase != BattlePhase.None && Phase != BattlePhase.Ended;

        private byte _myPosition;
        public byte MyPosition { get => _myPosition; set { _myPosition = value; Notify(); } }

        private int _roundCount;
        public int RoundCount { get => _roundCount; set { _roundCount = value; Notify(); } }

        public ObservableCollection<BattleEntity> Allies { get; } = new();
        public ObservableCollection<BattleEntity> Enemies { get; } = new();

        public BattleState()
        {
            BindingOperations.EnableCollectionSynchronization(Allies, _alliesLock);
            BindingOperations.EnableCollectionSynchronization(Enemies, _enemiesLock);
        }

        public void Reset()
        {
            Phase = BattlePhase.None;
            MyPosition = 0;
            RoundCount = 0;
            Allies.Clear();
            Enemies.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
