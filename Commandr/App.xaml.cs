using System.Windows;
using Framework;

namespace Commandr
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Antes de crear la ventana: VM.BasePath se arma con Configuration.Default.ProjectPath.
            LegacySettings.Import("Commandr.exe", Commandr.Properties.Settings.Default, Commandr.Configuration.Default);

            base.OnStartup(e);
        }
    }
}
