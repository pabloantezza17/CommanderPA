using System.Windows;
using Framework;

namespace MailSpy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LegacySettings.Import("MailSpy.exe", MailSpy.Configuration.Default, MailSpy.Properties.Settings.Default);

            base.OnStartup(e);
        }
    }
}
