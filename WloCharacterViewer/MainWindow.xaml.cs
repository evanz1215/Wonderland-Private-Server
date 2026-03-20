using System.Windows;
using System.Windows.Controls;
using WloCharacterViewer.ViewModels;

namespace WloCharacterViewer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PasswordBox.Password = "123456";
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }

    private void SlotCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && SlotCombo.SelectedIndex >= 0)
            vm.SelectedSlot = (byte)(SlotCombo.SelectedIndex + 1);
    }
}
