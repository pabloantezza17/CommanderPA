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

        // El get latest emite salida continua (un archivo por línea). Si se queda sin actividad
        // este tiempo, damos por colgado el proceso (VPN caída o merge esperando respuesta) y lo matamos.
        private const Int32 InactivityTimeoutMs = 120000;

        // Marca de tiempo (Ticks) de la última línea recibida del proceso.
        private long _lastOutputTicks;

        // Se enciende si la salida del tf get reporta conflictos (para abrir el merge manual).
        private volatile Boolean _conflictsDetected;

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

            this._conflictsDetected = false;

            Process proc = null;

            try
            {
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/s /c \"" + command + "\"",
                        WorkingDirectory = WorkspaceDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true
                    }
                };

                Interlocked.Exchange(ref this._lastOutputTicks, DateTime.UtcNow.Ticks);

                // Lectura asíncrona de ambos flujos para evitar deadlocks por buffers llenos.
                // Cada línea recibida refresca la marca de actividad para el watchdog.
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) { vm.AddLine(e.Data); Interlocked.Exchange(ref this._lastOutputTicks, DateTime.UtcNow.Ticks); if (LooksLikeConflict(e.Data)) this._conflictsDetected = true; } };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) { vm.AddLine(e.Data); Interlocked.Exchange(ref this._lastOutputTicks, DateTime.UtcNow.Ticks); if (LooksLikeConflict(e.Data)) this._conflictsDetected = true; } };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // Cerramos stdin: si tf pide algo por consola recibe EOF en vez de colgarse.
                try { proc.StandardInput.Close(); } catch { }

                while (!proc.WaitForExit(1000))
                {
                    long idleMs = (DateTime.UtcNow.Ticks - Interlocked.Read(ref this._lastOutputTicks)) / TimeSpan.TicksPerMillisecond;

                    if (idleMs >= InactivityTimeoutMs)
                    {
                        vm.AddLine(String.Format(
                            "Sin actividad por {0}s. Se cancela (¿VPN caída o merge esperando respuesta?).",
                            InactivityTimeoutMs / 1000));

                        KillProcessTree(proc.Id);
                        vm.Finish(false);
                        return;
                    }
                }

                // Si hubo conflictos, abrimos una consola interactiva para resolver el merge
                // (como pasaba antes con el .bat) y esperamos a que se cierre.
                if (this._conflictsDetected)
                    this.ResolveConflictsInteractively(vm);

                vm.Finish(proc.ExitCode == 0);
            }
            catch (Exception ex)
            {
                vm.AddLine("No se pudo iniciar el get latest: " + ex.Message);
                vm.Finish(false);
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }
        }

        /// <summary>
        /// Detecta si una línea de salida de tf reporta un conflicto de merge.
        /// "conflict" cubre tanto el inglés ("Conflict ...") como el español ("Conflicto ...").
        /// </summary>
        private static Boolean LooksLikeConflict(String line)
        {
            return line != null && line.IndexOf("conflict", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Abre una consola visible con "tf resolve /recursive" para que el usuario resuelva el
        /// merge a mano (con la herramienta de merge de VS). Bloquea hasta que se cierra la consola.
        /// </summary>
        private void ResolveConflictsInteractively(TfsVM vm)
        {
            vm.AddLine("Se detectaron conflictos. Abriendo consola para resolver el merge...");

            try
            {
                // /k mantiene la consola abierta tras el resolve para ver el resultado; el usuario
                // la cierra (o escribe exit) y recién ahí terminamos el get latest.
                String command = String.Format(
                    "set \"VSCMD_START_DIR={1}\" && call \"{0}\" && cd /d \"{1}\" && tf resolve /recursive",
                    VsDevCmdPath, WorkspaceDir);

                Process proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/k \"" + command + "\"",
                    WorkingDirectory = WorkspaceDir,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });

                if (proc != null)
                {
                    proc.WaitForExit();
                    proc.Dispose();
                }

                vm.AddLine("Consola de merge cerrada.");
            }
            catch (Exception ex)
            {
                vm.AddLine("No se pudo abrir la consola de merge: " + ex.Message);
            }
        }

        /// <summary>
        /// Mata el proceso y toda su descendencia. En .NET Framework no existe
        /// Process.Kill(entireProcessTree), así que delegamos en taskkill para no dejar
        /// huérfano al tf.exe que cuelga de cmd.exe.
        /// </summary>
        private static void KillProcessTree(Int32 pid)
        {
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
                try { Process.GetProcessById(pid).Kill(); } catch { }
            }
        }

        #endregion
    }
}
