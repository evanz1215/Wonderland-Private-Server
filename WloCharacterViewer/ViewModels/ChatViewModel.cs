using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WloCharacterViewer.Models;
using WloCharacterViewer.Network;

namespace WloCharacterViewer.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly GameSession _session;

        public ObservableCollection<ChatMessage> Messages => _session.ChatMessages;

        private string _inputText = "";
        public string InputText { get => _inputText; set { _inputText = value; Notify(); } }

        private ChatChannel _selectedChannel = ChatChannel.Local;
        public ChatChannel SelectedChannel { get => _selectedChannel; set { _selectedChannel = value; Notify(); } }

        private string _whisperTarget = "";
        public string WhisperTarget { get => _whisperTarget; set { _whisperTarget = value; Notify(); } }

        public ICommand SendCommand { get; }

        public ChatViewModel(GameSession session)
        {
            _session = session;
            SendCommand = new RelayCommand(_ => SendMessage(), _ => !string.IsNullOrWhiteSpace(InputText));
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            var pw = new PacketWriter();
            pw.Pack8(2);

            switch (SelectedChannel)
            {
                case ChatChannel.Whisper:
                    pw.Pack8(1);
                    pw.PackStringN(WhisperTarget);
                    pw.PackStringN(InputText);
                    break;
                case ChatChannel.Local:
                    pw.Pack8(2);
                    pw.PackStringN(InputText);
                    break;
                case ChatChannel.Party:
                    pw.Pack8(3);
                    pw.PackStringN(InputText);
                    break;
                case ChatChannel.World:
                    pw.Pack8(5);
                    pw.PackStringN(InputText);
                    break;
                default:
                    return;
            }

            _session.SendPacket(pw.Build());
            InputText = "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
