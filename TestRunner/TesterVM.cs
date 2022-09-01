using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TestRunner
{
    public class TesterVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<TestEntity> _failed;
        private ObservableCollection<TestEntity> _integration;
        private Stopwatch _stopwatch;
        private StringBuilder Builder;

        #endregion

        #region Constructor

        public TesterVM()
        {
            this.Builder = new StringBuilder();

            this.InicializarColores();
        }

        private void InicializarColores()
        {
            this.ColorGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44D449"));

            this.ColorYellow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDEA2B"));

            this.ColorRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EB3B3B"));
        }

        #endregion

        #region Properties

        public Boolean ShowProgress
        {
            get { return Settings.Default.ShowProgress; }
            set { Settings.Default.ShowProgress = value; this.SaveSettings(); }
        }

        public String Rama { get; set; }

        public Stopwatch Stopwatch
        {
            get
            {
                if (this._stopwatch == null)
                    this._stopwatch = new Stopwatch();

                return this._stopwatch;
            }
        }

        public Double ElapsedTime
        {
            get
            {
                return Math.Floor(this.Stopwatch.Elapsed.TotalSeconds);
            }
        }

        public Int32 Count
        {
            get
            {
                return this.FailedTestsCollection.Count
                +
                this.PassedTestsCounter;
            }
        }

        public Visibility ShowTabs
        {
            get
            {
                var count = this.Count > 0;
                return count ?
                    Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility ShowLoading
        {
            get
            {
                return this.Count > 0 ?
                    Visibility.Collapsed : Visibility.Visible;
            }
        }

        public String CantPassed
        {
            get
            {
                return "Passed (" + this.PassedTestsCounter + ")";
            }
        }

        public String CantFailed
        {
            get
            {
                return "Failed (" + this.FailedTestsCollection.Count + ")";
            }
        }

        public String CantIntegration
        {
            get
            {
                return "Integration (" + this.IntegrationTestsCollection.Count + ")";
            }
        }

        public String Title
        {
            get
            {
                String message = this.Rama + " - " + this.AllCant;

                if (this.Stopwatch.IsRunning || this.Stopwatch.ElapsedMilliseconds > 0)
                    message += ". Elapsed: " + this.ElapsedTime + "s";

                return message;
            }
        }

        public String AllCant
        {
            get
            {
                return "Tests ran: " + (this.PassedTestsCounter + this.FailedTestsCollection.Count).ToString();
            }
        }

        public ObservableCollection<TestEntity> FailedTestsCollection
        {
            get
            {
                if (this._failed == null)
                    this._failed = new ObservableCollection<TestEntity>();

                return this._failed;
            }
        }

        public ObservableCollection<TestEntity> IntegrationTestsCollection
        {
            get
            {
                if (this._integration == null)
                    this._integration = new ObservableCollection<TestEntity>();

                return this._integration;
            }
        }

        public Int32 PassedTestsCounter { get; set; }

        public Brush PassedTestCounterColor
        {
            get
            {
                if (this.FailedTestsCollection.Count > 0)
                    return this.ColorRed;

                return this.ColorGreen;
            }
        }

        public String Gif
        {
            get
            {
                return this.GetRandomFileFromDirectory("gifs");
            }
        }

        private Brush ColorGreen { get; set; }
        private Brush ColorRed { get; set; }
        private Brush ColorYellow { get; set; }

        #endregion

        #region Methods

        public String GetRandomFileFromDirectory(String localdir)
        {
            String dir = Path.Combine(Environment.CurrentDirectory, localdir);

            var files = Directory.EnumerateFiles(dir);
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            return files.ElementAt(rnd.Next(files.Count()));
        }

        protected void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaiseProps()
        {
            this.RaisePropertyChangedEvent("Title");

            if (this.ShowProgress)
            {
                this.RaisePropertyChangedEvent("CantPassed");
                this.RaisePropertyChangedEvent("CantIntegration");
                this.RaisePropertyChangedEvent("CantFailed");
                this.RaisePropertyChangedEvent("PassedTestsCounter");
                this.RaisePropertyChangedEvent("PassedTestCounterColor");
                this.RaisePropertyChangedEvent("FailedTestsCollection");
                this.RaisePropertyChangedEvent("IntegrationTestsCollection");
            }

            if (this.Count < 100)
            {
                this.RaisePropertyChangedEvent("ShowTabs");
                this.RaisePropertyChangedEvent("ShowLoading");
            }
        }

        private void InvokeUpdateList(ObservableCollection<TestEntity> list, String line)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => list.Add(new TestEntity(line))));
            this.RaiseProps();
        }

        public Boolean AddLine(String line)
        {
            this.Builder.AppendLine(line);

            if (line.Contains("Integration"))
            {
                this.InvokeUpdateList(this.IntegrationTestsCollection, line);
                return true;
            }
            else if (line.StartsWith("Passed"))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => this.PassedTestsCounter++));
                this.RaiseProps();
                return true;
            }
            else if (line.StartsWith("Failed"))
            {
                this.InvokeUpdateList(this.FailedTestsCollection, line);
                return true;
            }
            else
                return false;
        }

        public void Finish()
        {
            this.Stopwatch.Stop();

            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            StreamWriter writer = new StreamWriter(String.Format("logs/{0}-{1}.log", this.Rama, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")));

            writer.WriteLine(String.Format("Ran {0} tests in {1} seconds.", this.Count, this.ElapsedTime));
            writer.Write(this.Builder.ToString());
            writer.Flush();
            writer.Close();

            this.ShowProgress = true;

            this.RaiseProps();

            this.CleanUp();

            MessageBox.Show(String.Format("Test run finished! Time elapsed: {0}s", this.ElapsedTime), this.AllCant);
        }

        public void CleanUp()
        {
            try
            {
                if (Directory.Exists("TestResults"))
                    Directory.Delete("TestResults", true);
            }
            catch (Exception)
            {
            }
        }

        public void SaveSettings()
        {
            Settings.Default.Save();
        }

        #endregion
    }
}