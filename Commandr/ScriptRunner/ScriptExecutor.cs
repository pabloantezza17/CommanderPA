using System;
using System.Diagnostics;
using System.IO;

namespace Commandr.ScriptRunner
{
    /// <summary>
    /// Ejecuta un único comando (un .bat vía cmd.exe, o un ejecutable directo) redirigiendo su
    /// salida en vivo a la <see cref="ScriptVM"/>. Reemplaza la consola negra de cmd que se abría
    /// antes al lanzar estos scripts, mostrando la salida dentro de una ventana propia.
    /// </summary>
    public class ScriptExecutor
    {
        #region Members

        private readonly ScriptVM VM;
        private readonly String path;
        private readonly Boolean isBatch;
        private readonly String successText;

        #endregion

        #region Constructor

        /// <param name="path">Ruta al .bat o al ejecutable (o nombre en PATH, ej. "iisreset").</param>
        /// <param name="isBatch">True si <paramref name="path"/> es un .bat (se invoca con cmd.exe /c).</param>
        /// <param name="successText">Texto de estado cuando termina bien.</param>
        public ScriptExecutor(ScriptVM vm, String path, Boolean isBatch, String successText)
        {
            this.VM = vm;
            this.path = path;
            this.isBatch = isBatch;
            this.successText = successText;
        }

        #endregion

        #region Methods

        public void Run()
        {
            this.VM.Stopwatch.Start();

            try
            {
                if (this.isBatch && !File.Exists(this.path))
                {
                    this.VM.AddLine("No se encontró el script:");
                    this.VM.AddLine(this.path);
                    this.VM.Finish(false, "No se encontró el script");
                    return;
                }

                Boolean ok = this.RunProcess();

                this.VM.Finish(ok, ok ? this.successText : "El proceso terminó con errores");
            }
            catch (Exception ex)
            {
                this.VM.AddLine("Error: " + ex.Message);
                this.VM.Finish(false, "No se pudo ejecutar");
            }
        }

        private Boolean RunProcess()
        {
            String fileName;
            String arguments;
            String workingDirectory;

            if (this.isBatch)
            {
                // Los .bat no son ejecutables directos: para redirigir la salida hay que invocarlos
                // a través de cmd.exe /c. El directorio de trabajo es el de la propia rutina.
                fileName = "cmd.exe";
                arguments = "/c \"" + this.path + "\"";
                workingDirectory = Path.GetDirectoryName(this.path);
            }
            else
            {
                fileName = this.path;
                arguments = String.Empty;
                workingDirectory = Path.GetDirectoryName(this.path);

                // Para comandos resueltos por PATH (ej. "iisreset") no hay directorio: uso el actual.
                if (String.IsNullOrEmpty(workingDirectory))
                    workingDirectory = Directory.GetCurrentDirectory();
            }

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
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };

            // Lectura asíncrona de ambos flujos para evitar deadlocks por buffers llenos.
            proc.OutputDataReceived += (s, e) => { if (e.Data != null) this.VM.AddLine(CleanLine(e.Data)); };
            proc.ErrorDataReceived += (s, e) => { if (e.Data != null) this.VM.AddLine(CleanLine(e.Data)); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // Cierro el stdin: los .bat que terminan en "pause" (o cualquier prompt) reciben EOF
            // y continúan en vez de quedarse esperando una tecla para siempre.
            proc.StandardInput.Close();

            proc.WaitForExit();

            return proc.ExitCode == 0;
        }

        /// <summary>
        /// Reemplaza el mensaje del "pause" del .bat ("Presione una tecla para continuar . . ." /
        /// "Press any key to continue . . .") por un texto más claro.
        /// </summary>
        private static String CleanLine(String line)
        {
            if (line.IndexOf("tecla para continuar", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("any key to continue", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Finalizado";

            return line;
        }

        #endregion
    }
}
