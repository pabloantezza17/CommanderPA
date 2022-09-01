using System;
using System.Diagnostics;
using System.IO;
using Framework;

namespace Commandr
{
    public class Builder
    {
        private readonly String buildString = "\"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"{0}\"  /v:q /m /nr:false /p:WarningLevel=0;Configuration=Debug;Optimize=false /clp:ErrorsOnly /nologo";

        private String tempFileName;

        public void Build(FileCommand fileCommand, String name, String currentBranch)
        {
            this.tempFileName = Utils.GetTempFilePathWithExtension(".bat");
            StreamWriter writer = new StreamWriter(this.tempFileName);

            writer.WriteLine(@"
@echo off
setlocal

set STARTTIME=%TIME%

echo -------------------------------------------
echo Deteniendo FyOCorSchedulerWinService
echo -------------------------------------------
net stop FyOCorSchedulerWinService 2>nul
echo.
");

            //if (this.modules.Count(m => m.Value.Enabled) == 0)
            //{
            //    MessageBox.Show("There are no enabled modules to build!", "Build Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}

            //foreach (var module in this.modules.Where(m => m.Value.Enabled))
            //{
            //    writer.WriteLine("echo -------------------------------------------");
            //    writer.WriteLine("echo : : Building: " + module.Value.Name);
            //    writer.WriteLine("echo -------------------------------------------");
            //    writer.WriteLine(String.Format(this.buildString, module.Value.Data));
            //}

            writer.WriteLine("echo -------------------------------------------");
            writer.WriteLine("echo : : Building: " + name);
            writer.WriteLine("echo -------------------------------------------");
            writer.WriteLine(String.Format(this.buildString, Path.GetFullPath(String.Format(fileCommand.Command, currentBranch))));

            writer.WriteLine(@"
            echo Started: %STARTTIME%
            set ENDTIME=%TIME%
            echo Ended: %ENDTIME%

            rem convert STARTTIME and ENDTIME to centiseconds
            set /A STARTTIME=(1%STARTTIME:~0,2%-100)*360000 + (1%STARTTIME:~3,2%-100)*6000 + (1%STARTTIME:~6,2%-100)*100 + (1%STARTTIME:~9,2%-100)
            set /A ENDTIME=(1%ENDTIME:~0,2%-100)*360000 + (1%ENDTIME:~3,2%-100)*6000 + (1%ENDTIME:~6,2%-100)*100 + (1%ENDTIME:~9,2%-100)

            set /A DURATION=%ENDTIME%-%STARTTIME%
            set /A DURATIONH=%DURATION% / 360000
            set /A DURATIONM=(%DURATION% - %DURATIONH%*360000) / 6000
            set /A DURATIONS=(%DURATION% - %DURATIONH%*360000 - %DURATIONM%*6000) / 100

            echo Elapsed: %DURATIONH%h %DURATIONM%m %DURATIONS%s

");

            writer.WriteLine("pause");

            writer.Flush();
            writer.Close();

            Process proc = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.tempFileName
                }
            };

            proc.Start();

            proc.Exited += Proc_Exited;
        }

        private void Proc_Exited(Object sender, EventArgs e)
        {
            File.Delete(tempFileName);
        }
    }
}