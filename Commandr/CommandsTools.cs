using System;
using System.Diagnostics;
using System.IO;
using Framework;

namespace Commandr
{
    public class CommandsTools
    {
        private String tempFileName;

        public void RunAsVS2017()
        {
            this.RunCommand(String.Format("runas /netonly /user:{0} \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Professional\\Common7\\IDE\\devenv.exe\"", Configuration.Default.User));
        }
        
        public void RunAsVS2019()
        {
            this.RunCommand(String.Format("runas /netonly /user:{0} \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Professional\\Common7\\IDE\\devenv.exe\"", Configuration.Default.User));
        }

        public void RunAsSQL()
        {
            this.RunCommand(String.Format("runas /netonly /user:{0} \"C:\\Program Files (x86)\\Microsoft SQL Server\\120\\Tools\\Binn\\ManagementStudio\\Ssms.exe\"", Configuration.Default.User));
        }

        private void RunCommand(String commandString)
        {
            this.tempFileName = Utils.GetTempFilePathWithExtension(".bat");
            StreamWriter writer = new StreamWriter(this.tempFileName);

            writer.WriteLine(commandString);

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