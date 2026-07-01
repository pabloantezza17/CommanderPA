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
        Failed
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

        public void Finish(Boolean success, String statusText)
        {
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
                        success ? "OK" : "ERRORES",
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
