using System.Windows;

namespace TestRunner
{
    public partial class ShowSettings : Window
    {
        public ShowSettings()
        {
            InitializeComponent();

            this.showProgress.IsChecked = Settings.Default.ShowProgress;
        }

        private void saveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowProgress = this.showProgress.IsChecked ?? false;
            Settings.Default.Save();

            this.Close();
        }
    }
}