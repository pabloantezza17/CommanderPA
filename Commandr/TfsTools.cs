using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Commandr.TfsRunner;

namespace Commandr
{
    public class TfsTools
    {
        #region Members

        private const String ServerRoot = "$/Corretaje";
        private const String WorkspaceDir = "C:\\Project\\FyO\\Corretaje";

        private const String VsDevCmdPath =
            "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\Tools\\VsDevCmd.bat";

        #endregion

        #region Methods

        public void GetLatestVersion(IEnumerable<String> branches)
        {
            var serverPaths = (branches ?? Enumerable.Empty<String>())
                .Where(b => !String.IsNullOrWhiteSpace(b))
                .Select(b => String.Format("{0}/{1}", ServerRoot, b.Trim()))
                .ToList();

            // Si no se eligió ninguna rama puntual, traemos toda la raíz del proyecto.
            String target = serverPaths.Count > 0 ? String.Join(" ", serverPaths) : ServerRoot;

            TfsVM vm = new TfsVM { Target = target };

            TfsWindow window = new TfsWindow(vm);
            window.Show();

            new Thread(() => this.RunGetLatest(vm, target)) { IsBackground = true }.Start();
        }

        private void RunGetLatest(TfsVM vm, String target)
        {
            vm.Stopwatch.Start();

            // tf requiere el entorno del Developer Command Prompt (VsDevCmd deja tf en el PATH),
            // por eso encadenamos la inicialización, el cd al workspace y el tf get en un cmd /c.
            String command = String.Format(
                "set \"VSCMD_START_DIR={1}\" && call \"{0}\" && cd /d \"{1}\" && tf get {2} /recursive /noprompt",
                VsDevCmdPath, WorkspaceDir, target);

            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/s /c \"" + command + "\"",
                        WorkingDirectory = WorkspaceDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Lectura asíncrona de ambos flujos para evitar deadlocks por buffers llenos.
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) vm.AddLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) vm.AddLine(e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                vm.Finish(proc.ExitCode == 0);
            }
            catch (Exception ex)
            {
                vm.AddLine("No se pudo iniciar el get latest: " + ex.Message);
                vm.Finish(false);
            }
        }

        #endregion
    }
}
