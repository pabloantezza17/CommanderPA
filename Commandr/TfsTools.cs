using System;
using System.Diagnostics;
using System.IO;
using Framework;

namespace Commandr
{
    public class TfsTools
    {
        private String tempFileName;

        public void GetLatestVersion()
        {
            this.tempFileName = Utils.GetTempFilePathWithExtension(".bat");
            StreamWriter writer = new StreamWriter(this.tempFileName);

            writer.WriteLine("@echo off");
            writer.WriteLine(String.Format("set VSCMD_START_DIR={0}", "\"C:\\Projects\\FyO\\Corretaje\""));

            writer.WriteLine(String.Format("call {0}", "\"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\Tools\\VsDevCmd.bat\""));
            writer.WriteLine("echo.");
            writer.WriteLine("echo Getting latest version...");
            writer.WriteLine("echo.");

            writer.WriteLine(@"
                            tf get
                            pause");

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