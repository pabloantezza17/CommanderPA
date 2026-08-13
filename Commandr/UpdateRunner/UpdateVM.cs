using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace Commandr.UpdateRunner
{
    public enum UpdateStatus
    {
        Working,
        Success,
        Failed,
        Cancelled
    }

    public class UpdateVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<BuildStep> _steps;
        private ObservableCollection<String> _output;
        private Stopwatch _stopwatch;
        private StringBuilder _log;
        private UpdateStatus _status;
        private String _statusText;
        private volatile Boolean _cancelled;

        /// <summary>
        /// Se dispara cuando se cierra la ventana con el proceso en curso.
        /// El <see cref="UpdateManager"/> lo escucha para matar el proceso vigente.
        /// </summary>
        public event EventHandler CancelRequested;

        #endregion

        #region Constructor

        public UpdateVM()
        {
            this._log = new StringBuilder();
            this._status = UpdateStatus.Working;
            this._statusText = "Iniciando...";
        }

        #endregion

        #region Properties

        public Boolean Cancelled
        {
            get { return this._cancelled; }
        }

        public String Branch { get; set; }

        public ObservableCollection<BuildStep> Steps
        {
            get { return this._steps ?? (this._steps = new ObservableCollection<BuildStep>()); }
        }

        public ObservableCollection<String> Output
        {
            get { return this._output ?? (this._output = new ObservableCollection<String>()); }
        }

        public UpdateStatus Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                this.RaisePropertyChangedEvent("Status");
                this.RaisePropertyChangedEvent("ShowWorking");
                this.RaisePropertyChangedEvent("ShowCancel");
                this.RaisePropertyChangedEvent("ShowCancelled");
            }
        }

        public String StatusText
        {
            get { return this._statusText; }
            set
            {
                this._statusText = value;
                this.RaisePropertyChangedEvent("StatusText");
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

        public String Title
        {
            get { return "Update " + this.Branch + " - " + this.ElapsedTime + "s"; }
        }

        public Visibility ShowWorking
        {
            get { return this.Status == UpdateStatus.Working ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowCancelled
        {
            get { return this.Status == UpdateStatus.Cancelled ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>El botón de frenar sólo tiene sentido mientras el proceso está en curso.</summary>
        public Visibility ShowCancel
        {
            get { return this.Status == UpdateStatus.Working ? Visibility.Visible : Visibility.Collapsed; }
        }

        #endregion

        #region Methods

        private void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaiseTime()
        {
            this.RaisePropertyChangedEvent("Title");
        }

        /// <summary>Ejecuta la acción en el hilo de UI (los cambios de estado vienen de un hilo de fondo).</summary>
        private static void OnUi(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        public void AddLine(String line)
        {
            this._log.AppendLine(line);

            OnUi(() => this.Output.Add(line));
        }

        public void SetStepState(BuildStep step, StepState state, String statusText = null)
        {
            OnUi(() =>
            {
                step.State = state;

                if (statusText != null)
                    this.StatusText = statusText;
            });
        }

        /// <summary>
        /// Cancela el proceso: marca el estado y avisa al <see cref="UpdateManager"/> para que
        /// mate lo que esté corriendo (tf, MSBuild, etc.).
        /// </summary>
        public void RequestCancel()
        {
            if (this._cancelled)
                return;

            this._cancelled = true;
            this.Stopwatch.Stop();

            // Primero mato el proceso vigente; después actualizo la pantalla.
            CancelRequested?.Invoke(this, EventArgs.Empty);

            this.AddLine("Proceso cancelado por el usuario.");

            this.WriteLog(false);

            OnUi(() =>
            {
                foreach (BuildStep step in this.Steps)
                {
                    if (step.State == StepState.Running)
                        step.State = StepState.Cancelled;
                    else if (step.State == StepState.Pending)
                        step.State = StepState.Skipped;
                }

                this.StatusText = "Cancelado por el usuario";
                this.Status = UpdateStatus.Cancelled;
            });
        }

        public void Finish(Boolean success, String statusText)
        {
            if (this._cancelled)
                return;

            this.Stopwatch.Stop();

            this.WriteLog(success);

            OnUi(() =>
            {
                this.StatusText = statusText;
                this.Status = success ? UpdateStatus.Success : UpdateStatus.Failed;
            });
        }

        private void WriteLog(Boolean success)
        {
            try
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                String fileName = String.Format(
                    "logs/update-{0}-{1}.log",
                    this.Branch,
                    DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(String.Format(
                        "Update + build + test de {0} - {1} en {2}s.",
                        this.Branch,
                        this._cancelled ? "CANCELADO" : (success ? "OK" : "ERRORES"),
                        this.ElapsedTime));

                    writer.Write(this._log.ToString());
                }
            }
            catch (Exception)
            {
                // El log es auxiliar: si no se puede escribir, no interrumpimos el flujo.
            }
        }

        #endregion
    }
}
