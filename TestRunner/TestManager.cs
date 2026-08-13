using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestRunner
{
    public class TestManager
    {
        #region Members

        private TesterVM VM;

        // La lista se toca desde los hilos del Parallel.ForEach y desde el hilo de UI (frenar /
        // cerrar la ventana), así que todo acceso va dentro del lock.
        private readonly List<Process> processes = new List<Process>();
        private readonly Object processGate = new Object();

        #endregion

        #region Constructor

        public TestManager(TesterVM vm)
        {
            this.VM = vm;

            this.VM.CancelRequested += this.VM_CancelRequested;
        }

        #endregion

        #region Methods

        public void RunTests(List<TestView> tests)
        {
            this.VM.Stopwatch.Start();

            // El botón de frenar depende de que el cronómetro esté corriendo.
            this.VM.RaiseRunState();

            this.CambiarOrdenDeEjecucion(tests);

            Int32 maxParallel = Settings.Default.MaxParallelTests > 0 ? Settings.Default.MaxParallelTests : 4;

            Parallel.ForEach(
                tests.Where(t => t.IsSelected),
                new ParallelOptions { MaxDegreeOfParallelism = maxParallel },
                t => this.DoTask(t)
                );

            this.VM.Stopwatch.Stop();

            this.KillProcesses();
            this.VM.Finish();
        }

        private void CambiarOrdenDeEjecucion(List<TestView> tests)
        {
            TestView entitiesTestView = tests.Where(t => t.Description.Contains("FyO.Cor.Business.Entities.Tests.Unit.dll")).FirstOrDefault();
            int entitiesTestViewIndex = tests.FindIndex(t => t.Description == entitiesTestView.Description);

            if (entitiesTestView != null && tests.Count() > 1)
            {
                tests.RemoveAt(entitiesTestViewIndex);
                tests.Insert(0, entitiesTestView);
            }
        }

        /// <summary>Mata los MSTest en curso cuando el usuario frena la corrida.</summary>
        private void VM_CancelRequested(Object sender, EventArgs e)
        {
            this.KillProcesses();
        }

        public void KillProcesses()
        {
            List<Process> running;

            lock (this.processGate)
            {
                running = new List<Process>(this.processes);
            }

            foreach (Process p in running)
                KillProcessTree(p);
        }

        /// <summary>
        /// Mata el proceso y toda su descendencia: MSTest levanta hosts hijos que quedarían
        /// huérfanos (y siguen consumiendo la máquina) con un Kill() simple.
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
                // El proceso pudo terminar entre el chequeo y la lectura del Id.
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

        private Process CreateProcess(TestView test)
        {
            Process p = new Process();

            lock (this.processGate)
            {
                this.processes.Add(p);
            }

            p.StartInfo.FileName = Settings.Default.MSTest;
            p.StartInfo.Arguments = String.Format(MainWindow.programSingleThread, test.Name);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            p.StartInfo.RedirectStandardOutput = true;

            return p;
        }

        private Object DoTask(Object o)
        {
            TestView testView = o as TestView;

            Thread.CurrentThread.IsBackground = true;

            // Frenaron la corrida: no arranco este assembly.
            if (this.VM.Cancelled)
                return null;

            // Categoría por assembly: Neoris.Fwk => pestaña FWK; *.Tests.Integration => pestaña
            // Integration; el resto suma al total. Se decide por el nombre del DLL, no por el
            // texto de cada línea.
            String assemblyName = System.IO.Path.GetFileName(testView.Name);
            Boolean esFwk = assemblyName.StartsWith("Neoris.Fwk");
            Boolean esIntegracion = !esFwk && assemblyName.EndsWith(".Tests.Integration.dll");

            Process process = this.CreateProcess(testView);
            process.Start();

            // La cancelación pudo llegar entre el chequeo anterior y el Start: la atiendo acá.
            if (this.VM.Cancelled)
            {
                KillProcessTree(process);
                return null;
            }

            String line;
            Boolean gettingResults = false;

            try
            {
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    // Dejo de procesar salida en cuanto se frena (el proceso ya fue matado).
                    if (this.VM.Cancelled)
                        return null;

                    if (!gettingResults)
                    {
                        if (line.StartsWith("-------"))
                            gettingResults = true;
                    }
                    else
                        gettingResults = this.VM.AddLine(line, esFwk, esIntegracion);
                }

                process.WaitForExit();
            }
            catch (Exception)
            {
                return null;
            }

            return testView;
        }

        #endregion
    }
}