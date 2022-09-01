using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Framework.UI;

namespace Helper
{
    public partial class HelperWindow : ObservableWindow
    {
        public HelperWindow()
        {
            this.InitializeComponent();

            this.DataContext = this;

            this.Ramas = new ObservableCollection<String> { "Devp_Corretaje" };

            this.Rama = this.Ramas.First();

            this.RaisePropertyChangedEvent("Ramas");
        }

        public ObservableCollection<String> Ramas { get; set; }

        public String Rama { get; set; }

        private void Btn_EncontrarParameters_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            new EncontrarParameters()
                .SetRama(this.Rama)
                .Show();

            this.Close();
        }

        private void Btn_EncontrarAsociacionEntreEmpresas_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            new EncontrarAsociacionEntreEmpresas()
                .SetRama(this.Rama)
                .Show();

            this.Close();
        }

        private void Btn_EncontrarRegexMatch_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            new EncontrarRegexMatch()
                .Show();

            this.Close();
        }

        private void Btn_Templater_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            new TemplaterWindow().Show();

            this.Close();
        }

        private void Btn_MailSpy_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            var currentDir = Directory.GetCurrentDirectory();

            Process.Start(new ProcessStartInfo
            {
                FileName = currentDir + "\\MailSpy\\MailSpy.exe",
                WorkingDirectory = currentDir + "\\MailSpy"
            });

            this.Close();
        }
    }
}