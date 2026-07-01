using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public void Run()
        {
            this.VM.Stopwatch.Start();

            // 1) Get latest (sólo en el flujo que lo incluye)
            if (this.includeGetLatest && !this.GetLatest())
            {
                this.VM.Finish(false, "Get latest falló");
                return;
            }

            // 2) Detengo el scheduler para que no bloquee DLLs durante la compilación.
            this.StopScheduler();

            // 3) Compilación de las soluciones en orden
            Int32 total = this.solutionSteps.Count;
            Int32 current = 0;

            foreach (var step in this.VM.Steps.Where(s => s.SolutionPath != null))
            {
                current++;

                this.VM.SetStepState(step, StepState.Running,
                    String.Format("Compilando {0} ({1} de {2})", step.Name, current, total));

                Boolean ok = this.BuildSolution(step.SolutionPath);

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
            this.LaunchTestRunner();
            this.VM.SetStepState(this.testStep, StepState.Ok);

            this.VM.Finish(true, "Todo compiló. TestRunner abierto.");
        }

        private Boolean GetLatest()
        {
            this.VM.SetStepState(this.getLatestStep, StepState.Running, "Obteniendo última versión...");

            var serverPaths = this.branchesToGet
                .Where(b => !String.IsNullOrWhiteSpace(b))
                .Select(b => String.Format("{0}/{1}", ServerRoot, b.Trim()))
                .ToList();

            String target = serverPaths.Count > 0 ? String.Join(" ", serverPaths) : ServerRoot;

            // tf requiere el entorno del Developer Command Prompt (VsDevCmd deja tf en el PATH).
            String command = String.Format(
                "set \"VSCMD_START_DIR={1}\" && call \"{0}\" && cd /d \"{1}\" && tf get {2} /recursive /noprompt",
                VsDevCmdPath, WorkspaceDir, target);

            Boolean ok = this.RunProcess("cmd.exe", "/s /c \"" + command + "\"", WorkspaceDir);

            this.VM.SetStepState(this.getLatestStep, ok ? StepState.Ok : StepState.Failed);

            return ok;
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

        private void LaunchTestRunner()
        {
            try
            {
                String testRunner = Path.Combine(this.workingDir, @"TestRunner\TestRunner.exe");

                Process.Start(new ProcessStartInfo
                {
                    FileName = testRunner,
                    Arguments = this.currentBranch,
                    WorkingDirectory = Path.GetDirectoryName(testRunner)
                });
            }
            catch (Exception ex)
            {
                this.VM.AddLine("No se pudo abrir el TestRunner: " + ex.Message);
            }
        }

        /// <summary>Corre un proceso con salida redirigida en vivo. Devuelve true si el exit code es 0.</summary>
        private Boolean RunProcess(String fileName, String arguments, String workingDirectory)
        {
            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Lectura asíncrona de ambos flujos para evitar deadlocks por buffers llenos.
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) this.VM.AddLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) this.VM.AddLine(e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                this.VM.AddLine("Error ejecutando " + fileName + ": " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}
