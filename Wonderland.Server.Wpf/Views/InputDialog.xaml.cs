using System.Windows;

namespace WonderlandServerWpf.Views
{
    public partial class InputDialog : Window
    {
        public string InputValue { get; private set; }

        public InputDialog(string title, string label, string defaultValue)
        {
            InitializeComponent();
            Title = title;
            Tag = title;
            LabelText.Text = label;
            InputBox.Text = defaultValue ?? "";
            InputBox.SelectAll();
            InputBox.Focus();
        }

        void OnOK(object sender, RoutedEventArgs e)
        {
            InputValue = InputBox.Text;
            DialogResult = true;
        }
    }
}
