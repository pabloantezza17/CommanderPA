using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace Commandr.TfsRunner
{
    public enum TfsStatus
    {
        Working,
        Success,
        Failed
    }

    public class TfsVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<String> _output;
        private Stopwatch _stopwatch;
        private StringBuilder _log;
        private TfsStatus _status;

        #endregion

        #region Constructor

        public TfsVM()
        {
            this._log = new StringBuilder();
            this._status = TfsStatus.Working;
        }

        #endregion

        #region Properties

        public String Target { get; set; }

        public TfsStatus Status
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

        public ObservableCollection<String> Output
        {
            get { return this._output ?? (this._output = new ObservableCollection<String>()); }
        }

        public String Title
        {
            get
            {
                String message = this.Target;
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
                    case TfsStatus.Success:
                        return "Get latest completado (" + this.Output.Count + " líneas)";
                    case TfsStatus.Failed:
                        return "Get latest con errores";
                    default:
                        return "Obteniendo última versión...";
                }
            }
        }

        public Visibility ShowWorking
        {
            get { return this.Status == TfsStatus.Working ? Visibility.Visible : Visibility.Collapsed; }
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
            this.RaisePropertyChangedEvent("ShowWorking");
        }

        public void AddLine(String line)
        {
            this._log.AppendLine(line);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Output.Add(line);
                this.RaisePropertyChangedEvent("StatusText");
            }));
        }

        public void Finish(Boolean success)
        {
            this.Stopwatch.Stop();

            this.WriteLog(success);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Status = success ? TfsStatus.Success : TfsStatus.Failed;
            }));
        }

        private void WriteLog(Boolean success)
        {
            try
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                String fileName = String.Format(
                    "logs/getlatest-{0}.log",
                    DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(String.Format(
                        "Get latest de {0} - {1} en {2}s.",
                        this.Target,
                        success ? "OK" : "ERRORES",
                        this.ElapsedTime));

                    writer.Write(this._log.ToString());
                }
            }
            catch (Exception)
            {
                // El log es auxiliar: si no se puede escribir, no interrumpimos el get latest.
            }
        }

        #endregion
    }
}
