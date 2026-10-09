using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CCcut
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        //private SettingsViewModel VM => this.DataContext as SettingsViewModel;
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = new SettingsViewModel(new WindowsFileDialogService());

            //this.Closed += (s, e) =>
            //{
            //    VM.SaveSettings();
            //};
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            // Prevent the framework from trying to handle the navigation internally
            e.Handled = true;
        }
    }
}
