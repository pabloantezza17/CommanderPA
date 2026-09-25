using System.Windows;
using Framework;

namespace TestRunner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LegacySettings.Import("TestRunner.exe", TestRunner.Settings.Default, TestRunner.Properties.Settings.Default);

            base.OnStartup(e);
        }
    }
}
