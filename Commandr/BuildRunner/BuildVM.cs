using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Commandr.BuildRunner
{
    public enum BuildStatus
    {
        Building,
        Success,
        Failed,
        Cancelled
    }

    public class BuildVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<BuildError> _errors;
        private Stopwatch _stopwatch;
        private StringBuilder _log;
        private BuildStatus _status;
        private volatile Boolean _cancelled;

        /// <summary>
        /// Se dispara cuando se cierra la ventana con la compilación en curso.
        /// El <see cref="Builder"/> lo escucha para matar el árbol de MSBuild.
        /// </summary>
        public event EventHandler CancelRequested;

        #endregion

        #region Constructor

        public BuildVM()
        {
            this._log = new StringBuilder();
            this._status = BuildStatus.Building;
        }

        #endregion

        #region Properties

        public Boolean Cancelled
        {
            get { return this._cancelled; }
        }

        public String SolutionName { get; set; }

        public String Branch { get; set; }

        public BuildStatus Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                this.RaiseAll();
            }
        }

        public Stopwatch Stopwatch
        {
            get { return this._stopwatch ?? (this._stopwatch = new Stopwatch()); }
        }

        public Double ElapsedTime
        {
            get { return Math.Floor(this.Stopwatch.Elapsed.TotalSeconds); }
        }

        public ObservableCollection<BuildError> Errors
        {
            get { return this._errors ?? (this._errors = new ObservableCollection<BuildError>()); }
        }

        public String Title
        {
            get
            {
                String message = this.SolutionName;

                if (!String.IsNullOrEmpty(this.Branch))
                    message += " (" + this.Branch + ")";

                message += " - " + this.ElapsedTime + "s";

                return message;
            }
        }

        public String StatusText
        {
            get
            {
                switch (this.Status)
                {
                    case BuildStatus.Success:
                        return "Compilación exitosa";
                    case BuildStatus.Failed:
                        return "Compilación con errores";
                    case BuildStatus.Cancelled:
                        return "Compilación cancelada";
                    default:
                        return "Compilando...";
                }
            }
        }

        public String CantErrors
        {
            get { return "Errores (" + this.Errors.Count + ")"; }
        }

        public Visibility ShowBuilding
        {
            get { return this.Status == BuildStatus.Building ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowSuccess
        {
            get { return this.Status == BuildStatus.Success ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowFailed
        {
            get { return this.Status == BuildStatus.Failed ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowCancelled
        {
            get { return this.Status == BuildStatus.Cancelled ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>El botón de frenar sólo tiene sentido mientras la compilación está en curso.</summary>
        public Visibility ShowCancel
        {
            get { return this.Status == BuildStatus.Building ? Visibility.Visible : Visibility.Collapsed; }
        }

        #endregion

        #region Methods

        protected void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaiseTime()
        {
            this.RaisePropertyChangedEvent("Title");
        }

        private void RaiseAll()
        {
            this.RaisePropertyChangedEvent("Title");
            this.RaisePropertyChangedEvent("StatusText");
            this.RaisePropertyChangedEvent("CantErrors");
            this.RaisePropertyChangedEvent("Errors");
            this.RaisePropertyChangedEvent("ShowBuilding");
            this.RaisePropertyChangedEvent("ShowSuccess");
            this.RaisePropertyChangedEvent("ShowFailed");
            this.RaisePropertyChangedEvent("ShowCancelled");
            this.RaisePropertyChangedEvent("ShowCancel");
        }

        public void AddLine(String line)
        {
            this._log.AppendLine(line);

            if (line.IndexOf(": error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Errors.Add(new BuildError(line));
                    this.RaisePropertyChangedEvent("CantErrors");
                    this.RaisePropertyChangedEvent("Errors");
                }));
            }
        }

        /// <summary>
        /// Cancela la compilación: marca el estado y avisa al <see cref="Builder"/> para que
        /// mate MSBuild. Idempotente (la ventana puede cerrarse una sola vez, pero por las dudas).
        /// </summary>
        public void RequestCancel()
        {
            if (this._cancelled)
                return;

            this._cancelled = true;
            this.Stopwatch.Stop();

            // Primero mato MSBuild; después actualizo la pantalla.
            CancelRequested?.Invoke(this, EventArgs.Empty);

            this.WriteLog(false);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Status = BuildStatus.Cancelled;
            }));
        }

        public void Finish(Boolean success)
        {
            if (this._cancelled)
                return;

            this.Stopwatch.Stop();

            this.WriteLog(success);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Si el proceso terminó con código de error pero no llegamos a parsear
                // ninguna línea de error, igual mostramos la pantalla de fallo.
                this.Status = success && this.Errors.Count == 0 ? BuildStatus.Success : BuildStatus.Failed;
            }));
        }

        private void WriteLog(Boolean success)
        {
            try
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                String fileName = String.Format(
                    "logs/build-{0}-{1}.log",
                    this.SolutionName,
                    DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(String.Format(
                        "Build de {0} ({1}) - {2} en {3}s.",
                        this.SolutionName,
                        this.Branch,
                        this._cancelled ? "CANCELADO" : (success ? "OK" : "ERRORES"),
                        this.ElapsedTime));

                    writer.Write(this._log.ToString());
                }
            }
            catch (Exception)
            {
                // El log es auxiliar: si no se puede escribir, no interrumpimos el build.
            }
        }

        #endregion
    }
}
