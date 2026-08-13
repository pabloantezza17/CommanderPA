using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Commandr.UpdateRunner
{
    /// <summary>
    /// Reimplementa el flujo del viejo update_build_and_test_vs2017.bat:
    /// get latest de las ramas, compilación en orden de las soluciones y, si todo pasa,
    /// se abre el TestRunner. Toda la salida se transmite en vivo a la <see cref="UpdateVM"/>.
    /// </summary>
    public class UpdateManager
    {
        #region Members

        private const String MSBuildPath =
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";

        private const String VsDevCmdPath =
            "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\Tools\\VsDevCmd.bat";

        private const String WorkspaceDir = "C:\\Project\\FyO\\Corretaje";
        private const String ServerRoot = "$/Corretaje";

        private const String buildArguments =
            "\"{0}\" /v:q /m /nr:true /p:WarningLevel=0;Configuration=Debug /clp:ErrorsOnly /nologo";

        // El get latest emite salida continua (un archivo por línea). Si se queda sin actividad
        // este tiempo, damos por colgado el proceso (VPN caída o merge esperando respuesta) y lo matamos.
        private const Int32 GetLatestInactivityMs = 120000;

        // Marca de tiempo (Ticks) de la última línea recibida del proceso vigilado.
        private long _lastOutputTicks;

        // Se enciende si la salida del tf get reporta conflictos (para abrir el merge manual).
        private volatile Boolean _conflictsDetected;

        // Orden de compilación tal cual el .bat (FWK y AppCore primero por dependencias).
        private static readonly Tuple<String, String>[] SolutionOrder =
        {
            Tuple.Create("FWK",     @"\src\Fwk\Neoris.FWK.sln"),
            Tuple.Create("AppCore", @"\src\AppCore\FyO.AppCore.sln"),
            Tuple.Create("Mae",     @"\src\FyO.Cor\FyO.Cor.Mae.sln"),
            Tuple.Create("Apc",     @"\src\FyO.Cor\FyO.Cor.Apc.sln"),
            Tuple.Create("Rie",     @"\src\FyO.Cor\FyO.Cor.Rie.sln"),
            Tuple.Create("Con",     @"\src\FyO.Cor\FyO.Cor.Con.sln"),
            Tuple.Create("Apl",     @"\src\FyO.Cor\FyO.Cor.Apl.sln"),
            Tuple.Create("Doc",     @"\src\FyO.Cor\FyO.Cor.Doc.sln"),
            Tuple.Create("Fac",     @"\src\FyO.Cor\FyO.Cor.Fac.sln"),
            Tuple.Create("Int",     @"\src\FyO.Cor\FyO.Cor.Int.sln"),
            Tuple.Create("Eai",     @"\src\FyO.Cor\FyO.Cor.Eai.sln"),
            Tuple.Create("Log",     @"\src\FyO.Cor\FyO.Cor.Log.sln"),
        };

        private readonly UpdateVM VM;
        private readonly List<String> branchesToGet;
        private readonly String currentBranch;
        private readonly String basePath;
        private readonly String workingDir;
        private readonly Boolean includeGetLatest;

        private BuildStep getLatestStep;
        private BuildStep testStep;
        private readonly Dictionary<BuildStep, String> solutionSteps = new Dictionary<BuildStep, String>();

        // Proceso que se está ejecutando ahora (para poder matarlo si cierran la ventana).
        private Process currentProcess;
        private readonly Object processGate = new Object();

        #endregion

        #region Constructor

        public UpdateManager(UpdateVM vm, IEnumerable<String> branchesToGet, String currentBranch, String basePath, String workingDir, Boolean includeGetLatest)
        {
            this.VM = vm;
            this.branchesToGet = (branchesToGet ?? Enumerable.Empty<String>()).ToList();
            this.currentBranch = currentBranch;
            this.basePath = basePath;
            this.workingDir = workingDir;
            this.includeGetLatest = includeGetLatest;

            this.VM.CancelRequested += this.VM_CancelRequested;

            this.BuildSteps();
        }

        #endregion

        #region Methods

        private void BuildSteps()
        {
            if (this.includeGetLatest)
            {
                this.getLatestStep = new BuildStep("Get Latest");
                this.VM.Steps.Add(this.getLatestStep);
            }

            foreach (var solution in SolutionOrder)
            {
                var step = new BuildStep(solution.Item1, this.basePath + solution.Item2);
                this.solutionSteps[step] = solution.Item1;
                this.VM.Steps.Add(step);
            }

            this.testStep = new BuildStep("Tests");
            this.VM.Steps.Add(this.testStep);
        }

        /// <summary>Mata el proceso vigente cuando se cierra la ventana.</summary>
        private void VM_CancelRequested(Object sender, EventArgs e)
        {
            lock (this.processGate)
            {
                if (this.currentProcess == null)
                    return;

                try
                {
                    if (!this.currentProcess.HasExited)
                        KillProcessTree(this.currentProcess.Id);
                }
                catch (Exception)
                {
                    // El proceso pudo terminar entre el chequeo y el kill: no hay nada que hacer.
                }
            }
        }

        public void Run()
        {
            this.VM.Stopwatch.Start();

            // 1) Get latest (sólo en el flujo que lo incluye).
            //    Un fallo acá NO es fatal: sin VPN (corte por inactividad) o con conflictos de
            //    auto-merge (tf get devuelve exit code != 0) igual seguimos a la compilación con
            //    el código que haya en el workspace. Sólo lo dejamos registrado como advertencia.
            if (this.includeGetLatest && !this.GetLatest())
            {
                this.VM.AddLine("El get latest no terminó bien; se continúa con la compilación de todos modos.");
            }

            if (this.VM.Cancelled)
                return;

            // 2) Detengo el scheduler para que no bloquee DLLs durante la compilación.
            this.StopScheduler();

            // 3) Compilación de las soluciones en orden
            Int32 total = this.solutionSteps.Count;
            Int32 current = 0;

            foreach (var step in this.VM.Steps.Where(s => s.SolutionPath != null))
            {
                // Cerraron la ventana: no arranco la siguiente solución.
                if (this.VM.Cancelled)
                    return;

                current++;

                this.VM.SetStepState(step, StepState.Running,
                    String.Format("Compilando {0} ({1} de {2})", step.Name, current, total));

                Boolean ok = this.BuildSolution(step.SolutionPath);

                if (this.VM.Cancelled)
                    return;

                if (!ok)
                {
                    this.VM.SetStepState(step, StepState.Failed);
                    this.VM.Finish(false, String.Format("Falló la compilación de {0}", step.Name));
                    return;
                }

                this.VM.SetStepState(step, StepState.Ok);
            }

            // 4) Tests
            this.VM.SetStepState(this.testStep, StepState.Running, "Abriendo TestRunner...");

            Boolean launched = this.LaunchTestRunner();

            this.VM.SetStepState(this.testStep, launched ? StepState.Ok : StepState.Failed);

            // Sólo el éxito cierra esta ventana sola: si el TestRunner no abrió, la dejamos
            // en pantalla para que se vea el error en el log.
            if (launched)
                this.VM.Finish(true, "Todo compiló. TestRunner abierto.");
            else
                this.VM.Finish(false, "Todo compiló, pero no se pudo abrir el TestRunner.");
        }

        private Boolean GetLatest()
        {
            this.VM.SetStepState(this.getLatestStep, StepState.Running, "Obteniendo última versión...");

            var serverPaths = this.branchesToGet
                .Where(b => !String.IsNullOrWhiteSpace(b))
                .Select(b => String.Format("{0}/{1}", ServerRoot, b.Trim()))
                .ToList();

            String target = serverPaths.Count > 0 ? String.Join(" ", serverPaths) : ServerRoot;

            this._conflictsDetected = false;

            // tf requiere el entorno del Developer Command Prompt (VsDevCmd deja tf en el PATH).
            String command = String.Format(
                "set \"VSCMD_START_DIR={1}\" && call \"{0}\" && cd /d \"{1}\" && tf get {2} /recursive /noprompt",
                VsDevCmdPath, WorkspaceDir, target);

            Boolean ok = this.RunProcess("cmd.exe", "/s /c \"" + command + "\"", WorkspaceDir, GetLatestInactivityMs);

            // Si hubo conflictos, abrimos una consola interactiva para que el usuario los resuelva
            // (como pasaba antes con el .bat) y esperamos a que la cierre antes de seguir.
            if (this._conflictsDetected)
                this.ResolveConflictsInteractively();

            this.VM.SetStepState(this.getLatestStep, ok ? StepState.Ok : StepState.Failed);

            return ok;
        }

        /// <summary>
        /// Abre una consola visible con "tf resolve /recursive" para que el usuario resuelva el
        /// merge a mano (con la herramienta de merge de VS). Bloquea hasta que se cierra la consola.
        /// </summary>
        private void ResolveConflictsInteractively()
        {
            this.VM.AddLine("Se detectaron conflictos. Abriendo consola para resolver el merge...");

            try
            {
                // /k mantiene la consola abierta tras el resolve para que se vea el resultado;
                // el usuario la cierra (o escribe exit) y recién ahí seguimos con la compilación.
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
                    lock (this.processGate)
                    {
                        this.currentProcess = proc;
                    }

                    if (this.VM.Cancelled)
                        this.VM_CancelRequested(this, EventArgs.Empty);

                    proc.WaitForExit();

                    lock (this.processGate)
                    {
                        this.currentProcess = null;
                        proc.Dispose();
                    }
                }

                this.VM.AddLine("Consola de merge cerrada. Se continúa con la compilación.");
            }
            catch (Exception ex)
            {
                this.VM.AddLine("No se pudo abrir la consola de merge: " + ex.Message);
            }
        }

        private void StopScheduler()
        {
            this.VM.AddLine("Deteniendo FyOCorSchedulerWinService...");
            this.RunProcess("net", "stop FyOCorSchedulerWinService", this.workingDir);
        }

        private Boolean BuildSolution(String solutionPath)
        {
            return this.RunProcess(
                MSBuildPath,
                String.Format(buildArguments, solutionPath),
                Path.GetDirectoryName(solutionPath));
        }

        /// <summary>Abre el TestRunner. Devuelve false si no se pudo lanzar.</summary>
        private Boolean LaunchTestRunner()
        {
            try
            {
                String testRunner = Path.Combine(this.workingDir, @"TestRunner\TestRunner.exe");

                Process proc = Process.Start(new ProcessStartInfo
                {
                    FileName = testRunner,
                    Arguments = this.currentBranch,
                    WorkingDirectory = Path.GetDirectoryName(testRunner)
                });

                if (proc == null)
                {
                    this.VM.AddLine("No se pudo abrir el TestRunner: no arrancó el proceso.");
                    return false;
                }

                proc.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                this.VM.AddLine("No se pudo abrir el TestRunner: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Corre un proceso con salida redirigida en vivo. Devuelve true si el exit code es 0.
        /// Si <paramref name="inactivityTimeoutMs"/> es mayor que cero, se vigila la actividad:
        /// si el proceso deja de emitir salida por ese lapso se lo da por colgado y se mata el árbol.
        /// </summary>
        private Boolean RunProcess(String fileName, String arguments, String workingDirectory, Int32 inactivityTimeoutMs = 0)
        {
            Process proc = null;

            try
            {
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
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
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) { this.VM.AddLine(e.Data); Interlocked.Exchange(ref this._lastOutputTicks, DateTime.UtcNow.Ticks); if (LooksLikeConflict(e.Data)) this._conflictsDetected = true; } };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) { this.VM.AddLine(e.Data); Interlocked.Exchange(ref this._lastOutputTicks, DateTime.UtcNow.Ticks); if (LooksLikeConflict(e.Data)) this._conflictsDetected = true; } };

                if (this.VM.Cancelled)
                    return false;

                lock (this.processGate)
                {
                    proc.Start();
                    this.currentProcess = proc;
                }

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // La cancelación pudo llegar entre el chequeo anterior y el Start: la atiendo acá.
                if (this.VM.Cancelled)
                    this.VM_CancelRequested(this, EventArgs.Empty);

                // Cerramos stdin: si el proceso (p. ej. tf) pide algo por consola recibe EOF
                // en vez de quedarse esperando input eternamente.
                try { proc.StandardInput.Close(); } catch { }

                if (inactivityTimeoutMs > 0)
                {
                    while (!proc.WaitForExit(1000))
                    {
                        long idleMs = (DateTime.UtcNow.Ticks - Interlocked.Read(ref this._lastOutputTicks)) / TimeSpan.TicksPerMillisecond;

                        if (idleMs >= inactivityTimeoutMs)
                        {
                            this.VM.AddLine(String.Format(
                                "Sin actividad por {0}s. Se cancela (¿VPN caída o merge esperando respuesta?).",
                                inactivityTimeoutMs / 1000));

                            KillProcessTree(proc.Id);
                            return false;
                        }
                    }
                }
                else
                {
                    proc.WaitForExit();
                }

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                this.VM.AddLine("Error ejecutando " + fileName + ": " + ex.Message);
                return false;
            }
            finally
            {
                lock (this.processGate)
                {
                    this.currentProcess = null;

                    if (proc != null) proc.Dispose();
                }
            }
        }

        /// <summary>
        /// Mata el proceso y toda su descendencia. En .NET Framework no existe
        /// Process.Kill(entireProcessTree), así que delegamos en taskkill para no dejar
        /// huérfano al tf.exe que cuelga de cmd.exe.
        /// </summary>
        /// <summary>
        /// Detecta si una línea de salida de tf reporta un conflicto de merge.
        /// "conflict" cubre tanto el inglés ("Conflict ...") como el español ("Conflicto ...").
        /// </summary>
        private static Boolean LooksLikeConflict(String line)
        {
            return line != null && line.IndexOf("conflict", StringComparison.OrdinalIgnoreCase) >= 0;
        }

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
