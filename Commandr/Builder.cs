using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Commandr.BuildRunner;

namespace Commandr
{
    public class Builder
    {
        #region Members

        private const String MSBuildPath =
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";

        private const String buildArguments =
            "\"{0}\" /v:q /m /nr:false /p:WarningLevel=0;Configuration=Debug;Optimize=false /clp:ErrorsOnly /nologo";

        private const String schedulerService = "FyOCorSchedulerWinService";

        #endregion

        #region Methods

        public void Build(FileCommand fileCommand, String name, String currentBranch)
        {
            String solutionPath = Path.GetFullPath(String.Format(fileCommand.Command, currentBranch));

            BuildVM vm = new BuildVM
            {
                SolutionName = name,
                Branch = currentBranch
            };

            BuildWindow window = new BuildWindow(vm);
            window.Show();

            new Thread(() => this.RunBuild(vm, solutionPath)) { IsBackground = true }.Start();
        }

        private void RunBuild(BuildVM vm, String solutionPath)
        {
            vm.Stopwatch.Start();

            this.StopScheduler();

            Process proc = null;
            Object gate = new Object();
            Boolean started = false;

            // Si cierran la ventana mientras compila, matamos MSBuild y sus nodos hijos.
            EventHandler onCancel = (s, e) =>
            {
                lock (gate)
                {
                    if (started)
                        KillProcessTree(proc);
                }
            };

            vm.CancelRequested += onCancel;

            try
            {
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = MSBuildPath,
                        Arguments = String.Format(buildArguments, solutionPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Lectura asíncrona de ambos flujos para evitar deadlocks por buffers llenos.
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) vm.AddLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) vm.AddLine(e.Data); };

                if (vm.Cancelled)
                    return;

                lock (gate)
                {
                    proc.Start();
                    started = true;
                }

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // La cancelación pudo llegar entre el chequeo anterior y el Start: la atiendo acá.
                if (vm.Cancelled)
                    KillProcessTree(proc);

                proc.WaitForExit();

                vm.Finish(proc.ExitCode == 0);
            }
            catch (Exception ex)
            {
                vm.AddLine("No se pudo iniciar la compilación: " + ex.Message);
                vm.Finish(false);
            }
            finally
            {
                vm.CancelRequested -= onCancel;

                lock (gate)
                {
                    if (proc != null)
                        proc.Dispose();
                }
            }
        }

        /// <summary>
        /// Mata el proceso y toda su descendencia. En .NET Framework no existe
        /// Process.Kill(entireProcessTree), así que delegamos en taskkill para no dejar
        /// huérfanos los nodos worker que levanta MSBuild con /m.
        /// </summary>
        private static void KillProcessTree(Process process)
        {
            Int32 pid;

            try
            {
                if (process == null || process.HasExited)
                    return;

                pid = process.Id;
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                Process kill = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = "/F /T /PID " + pid,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (kill != null)
                {
                    kill.WaitForExit();
                    kill.Dispose();
                }
            }
            catch (Exception)
            {
                // Último recurso: intento matar al menos el proceso raíz.
                try { process.Kill(); } catch { }
            }
        }

        private void StopScheduler()
        {
            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = "stop " + schedulerService,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception)
            {
                // El servicio puede no existir o requerir permisos: no bloqueamos el build por eso.
            }
        }

        #endregion
    }
}
