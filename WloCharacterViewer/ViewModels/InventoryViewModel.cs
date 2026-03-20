using System.ComponentModel;
using System.Runtime.CompilerServices;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        public InventoryState Inventory { get; }

        public InventoryViewModel(GameSession session)
        {
            Inventory = session.Inventory;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
