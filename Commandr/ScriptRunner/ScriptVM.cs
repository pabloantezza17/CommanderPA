using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Commandr.ScriptRunner
{
    public enum ScriptStatus
    {
        Working,
        Success,
        Failed
    }

    /// <summary>
    /// Estado observable de la ejecución de un único script/comando (bat o ejecutable).
    /// La salida se transmite en vivo a <see cref="Output"/> y el resultado final queda en <see cref="Status"/>.
    /// </summary>
    public class ScriptVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly String _title;
        private ObservableCollection<String> _output;
        private Stopwatch _stopwatch;
        private ScriptStatus _status;
        private String _statusText;

        #endregion

        #region Constructor

        public ScriptVM(String title)
        {
            this._title = title;
            this._status = ScriptStatus.Working;
            this._statusText = "Ejecutando...";
        }

        #endregion

        #region Properties

        public ObservableCollection<String> Output
        {
            get { return this._output ?? (this._output = new ObservableCollection<String>()); }
        }

        public ScriptStatus Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                this.RaisePropertyChangedEvent("Status");
                this.RaisePropertyChangedEvent("ShowWorking");
                this.RaisePropertyChangedEvent("IsDone");
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
            get { return this._title; }
        }

        public String Subtitle
        {
            get { return this.ElapsedTime + "s"; }
        }

        public Visibility ShowWorking
        {
            get { return this.Status == ScriptStatus.Working ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Boolean IsDone
        {
            get { return this.Status != ScriptStatus.Working; }
        }

        #endregion

        #region Methods

        private void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaiseTime()
        {
            this.RaisePropertyChangedEvent("Subtitle");
        }

        /// <summary>Ejecuta la acción en el hilo de UI (los cambios de estado vienen de un hilo de fondo).</summary>
        private static void OnUi(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        public void AddLine(String line)
        {
            OnUi(() => this.Output.Add(line));
        }

        public void Finish(Boolean success, String statusText)
        {
            this.Stopwatch.Stop();

            OnUi(() =>
            {
                this.StatusText = statusText;
                this.Status = success ? ScriptStatus.Success : ScriptStatus.Failed;
            });
        }

        #endregion
    }
}
