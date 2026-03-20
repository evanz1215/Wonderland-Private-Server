using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WloCharacterViewer.Models;
using WloCharacterViewer.ViewModels;

namespace WloCharacterViewer.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ChatViewModel vm && vm.SendCommand.CanExecute(null))
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ChannelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ChatViewModel vm && ChannelCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                vm.SelectedChannel = (ChatChannel)byte.Parse(tag);
            }
        }
    }
}
