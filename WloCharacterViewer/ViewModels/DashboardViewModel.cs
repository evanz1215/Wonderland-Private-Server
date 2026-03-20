using System.ComponentModel;
using System.Runtime.CompilerServices;
using WloCharacterViewer.Automation;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        public CharacterState Character { get; }
        public MapState Map { get; }
        public BotController Bot { get; }

        private bool _isConnected;
        public bool IsConnected { get => _isConnected; set { _isConnected = value; Notify(); } }

        public DashboardViewModel(GameSession session, BotController bot)
        {
            Character = session.Character;
            Map = session.Map;
            Bot = bot;
            IsConnected = true;

            session.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GameSession.IsInGame))
                    IsConnected = session.IsInGame;
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
