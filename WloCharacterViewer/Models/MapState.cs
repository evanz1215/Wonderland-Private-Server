using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WloCharacterViewer.Models
{
    public class MapState : INotifyPropertyChanged
    {
        private ushort _mapId;
        public ushort MapId { get => _mapId; set { _mapId = value; Notify(); } }

        private ushort _x;
        public ushort X { get => _x; set { _x = value; Notify(); } }

        private ushort _y;
        public ushort Y { get => _y; set { _y = value; Notify(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
