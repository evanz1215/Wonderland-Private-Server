using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WloCharacterViewer.Automation;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.ViewModels
{
    public class NavigationViewModel : INotifyPropertyChanged
    {
        private readonly GameSession _session;
        public MapState Map { get; }
        public AutoWalkEngine AutoWalk { get; }

        private ushort _wpMapId;
        public ushort WpMapId { get => _wpMapId; set { _wpMapId = value; Notify(); } }

        private ushort _wpX;
        public ushort WpX { get => _wpX; set { _wpX = value; Notify(); } }

        private ushort _wpY;
        public ushort WpY { get => _wpY; set { _wpY = value; Notify(); } }

        public ICommand AddWaypointCommand { get; }
        public ICommand ClearWaypointsCommand { get; }
        public ICommand UseCurrentPosCommand { get; }

        public NavigationViewModel(GameSession session, BotController bot)
        {
            _session = session;
            Map = session.Map;
            AutoWalk = bot.AutoWalk;

            AddWaypointCommand = new RelayCommand(_ =>
            {
                AutoWalk.Waypoints.Add(new Waypoint { MapId = WpMapId, X = WpX, Y = WpY });
            });

            ClearWaypointsCommand = new RelayCommand(_ =>
            {
                AutoWalk.Waypoints.Clear();
            });

            UseCurrentPosCommand = new RelayCommand(_ =>
            {
                WpMapId = Map.MapId;
                WpX = Map.X;
                WpY = Map.Y;
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
