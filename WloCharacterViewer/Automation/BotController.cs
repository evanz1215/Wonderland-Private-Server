using System.ComponentModel;
using System.Runtime.CompilerServices;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Automation
{
    public class BotController : INotifyPropertyChanged
    {
        public AutoHealEngine AutoHeal { get; } = new();
        public AutoBattleEngine AutoBattle { get; } = new();
        public AutoWalkEngine AutoWalk { get; } = new();

        public void Attach(GameSession session)
        {
            AutoHeal.Attach(session);
            AutoBattle.Attach(session);
            AutoWalk.Attach(session);
        }

        public void Detach()
        {
            AutoHeal.Detach();
            AutoBattle.Detach();
            AutoWalk.Detach();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
