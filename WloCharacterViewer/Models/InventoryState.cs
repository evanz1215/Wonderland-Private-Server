using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace WloCharacterViewer.Models
{
    public class InventoryItem : INotifyPropertyChanged
    {
        private byte _slot;
        public byte Slot { get => _slot; set { _slot = value; Notify(); } }

        private ushort _itemId;
        public ushort ItemId { get => _itemId; set { _itemId = value; Notify(); } }

        private ushort _quantity;
        public ushort Quantity { get => _quantity; set { _quantity = value; Notify(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class InventoryState : INotifyPropertyChanged
    {
        private static readonly object _bagLock = new();
        private static readonly object _equipLock = new();

        public ObservableCollection<InventoryItem> Bag { get; } = new();
        public ObservableCollection<InventoryItem> Equipment { get; } = new();

        public InventoryState()
        {
            BindingOperations.EnableCollectionSynchronization(Bag, _bagLock);
            BindingOperations.EnableCollectionSynchronization(Equipment, _equipLock);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
