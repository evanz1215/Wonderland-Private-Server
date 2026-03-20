using System;
using System.Windows;

namespace WonderlandServerWpf.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Ensure all background threads are terminated
            cGlobal.Run = false;
            try { System.Windows.Forms.Application.ExitThread(); } catch { }

            // Give threads a moment to exit gracefully, then force kill
            System.Threading.Thread.Sleep(500);
            Environment.Exit(0);
        }
    }
}
