using System.ComponentModel;
using System.Runtime.CompilerServices;
using WloCharacterViewer.Automation;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.ViewModels
{
    public class BattleBotViewModel : INotifyPropertyChanged
    {
        public BattleState Battle { get; }
        public AutoBattleEngine AutoBattle { get; }
        public AutoHealEngine AutoHeal { get; }

        public BattleBotViewModel(GameSession session, BotController bot)
        {
            Battle = session.Battle;
            AutoBattle = bot.AutoBattle;
            AutoHeal = bot.AutoHeal;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
