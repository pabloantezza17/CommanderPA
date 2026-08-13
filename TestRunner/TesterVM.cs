using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using TestRunner.Dialogs;

namespace TestRunner
{
    public class TesterVM : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<TestEntity> _failed;
        private ObservableCollection<TestEntity> _integration;
        private ObservableCollection<TestEntity> _fwk;
        private Stopwatch _stopwatch;
        private StringBuilder Builder;
        private volatile Boolean _cancelled;

        /// <summary>
        /// Se dispara cuando se frena la corrida.
        /// El <see cref="TestManager"/> lo escucha para matar los MSTest en curso.
        /// </summary>
        public event EventHandler CancelRequested;

        #endregion Members

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

        #endregion Constructor

        #region Properties

        public Boolean ShowProgress
        {
            get { return Settings.Default.ShowProgress; }
            set { Settings.Default.ShowProgress = value; this.SaveSettings(); }
        }

        public String Rama { get; set; }

        public Boolean Cancelled
        {
            get { return this._cancelled; }
        }

        /// <summary>El botón de frenar sólo tiene sentido mientras la corrida está en curso.</summary>
        public Visibility ShowCancel
        {
            get { return this.Stopwatch.IsRunning && !this._cancelled ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Cartel de corrida cancelada: reemplaza al de "corriendo" cuando se frenó antes de que
        /// llegara algún resultado (con resultados ya se ven las pestañas).
        /// </summary>
        public Visibility ShowCancelled
        {
            get { return this._cancelled && this.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }
        }

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
                return this.Count > 0 || this._cancelled ?
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

        public String CantFwk
        {
            get
            {
                return "FWK (" + this.FwkTestsCollection.Count + ")";
            }
        }

        public String Title
        {
            get
            {
                String message = this.Rama + " - " + this.AllCant;

                if (this.Stopwatch.IsRunning || this.Stopwatch.ElapsedMilliseconds > 0)
                    message += ". Tiempo: " + this.ElapsedTime + "s";

                if (this._cancelled)
                    message += " (cancelado)";

                return message;
            }
        }

        public String AllCant
        {
            get
            {
                return "Tests Totales: " + (this.PassedTestsCounter + this.FailedTestsCollection.Count + this.FwkTestsCollection.Count).ToString();
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

        public ObservableCollection<TestEntity> FwkTestsCollection
        {
            get
            {
                if (this._fwk == null)
                    this._fwk = new ObservableCollection<TestEntity>();

                return this._fwk;
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

        #endregion Properties

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
                this.RaisePropertyChangedEvent("CantFwk");
                this.RaisePropertyChangedEvent("FwkTestsCollection");
            }

            if (this.Count < 100)
            {
                this.RaisePropertyChangedEvent("ShowTabs");
                this.RaisePropertyChangedEvent("ShowLoading");
            }
        }

        /// <summary>Refresca lo que depende de si la corrida está viva o ya se frenó.</summary>
        public void RaiseRunState()
        {
            this.RaisePropertyChangedEvent("Title");
            this.RaisePropertyChangedEvent("ShowCancel");
            this.RaisePropertyChangedEvent("ShowCancelled");
            this.RaisePropertyChangedEvent("ShowTabs");
            this.RaisePropertyChangedEvent("ShowLoading");
        }

        /// <summary>
        /// Frena la corrida: congela el tiempo y avisa al <see cref="TestManager"/> para que mate
        /// los MSTest en curso. Los tests que faltaban no se arrancan.
        /// </summary>
        public void RequestCancel()
        {
            if (this._cancelled)
                return;

            this._cancelled = true;

            // Primero mato los procesos; después refresco la pantalla.
            CancelRequested?.Invoke(this, EventArgs.Empty);

            this.Stopwatch.Stop();

            this.RaiseRunState();
        }

        private void InvokeUpdateList(ObservableCollection<TestEntity> list, String line)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => list.Add(new TestEntity(line))));
            this.RaiseProps();
        }

        public Boolean AddLine(String line, Boolean esFwk, Boolean esIntegracion)
        {
            this.Builder.AppendLine(line);

            Boolean esResultado = line.StartsWith("Passed") || line.StartsWith("Failed");

            if (esFwk)
            {
                if (esResultado)
                {
                    this.InvokeUpdateList(this.FwkTestsCollection, line);
                    return true;
                }
                return false;
            }

            if (esIntegracion)
            {
                if (esResultado)
                {
                    this.InvokeUpdateList(this.IntegrationTestsCollection, line);
                    return true;
                }
                return false;
            }

            if (line.StartsWith("Passed"))
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

            writer.WriteLine(this._cancelled
                ? String.Format("CANCELADO: {0} tests alcanzaron a correr en {1} seconds.", this.Count, this.ElapsedTime)
                : String.Format("Ran {0} tests in {1} seconds.", this.Count, this.ElapsedTime));
            writer.Write(this.Builder.ToString());
            writer.Flush();
            writer.Close();

            this.ShowProgress = true;

            this.RaiseProps();

            this.RaiseRunState();

            this.CleanUp();

            this.ShowResultDialog();
        }

        /// <summary>Aviso de fin de corrida, con el color según cómo terminó.</summary>
        private void ShowResultDialog()
        {
            String title;
            DialogKind kind;

            if (this._cancelled)
            {
                title = "Corrida cancelada";
                kind = DialogKind.Stopped;
            }
            else if (this.FailedTestsCollection.Count > 0)
            {
                title = "Corrida terminada con fallas";
                kind = DialogKind.Failed;
            }
            else
            {
                title = "Corrida terminada";
                kind = DialogKind.Success;
            }

            MessageDialog.Info(
                title,
                String.Format("Tiempo transcurrido: {0}s", this.ElapsedTime),
                String.Format("{0}  ·  {1}  ·  {2}", this.AllCant, this.CantPassed, this.CantFailed),
                kind);
        }

        /// <summary>
        /// Borra los resultados de corridas anteriores. Un Delete recursivo se aborta entero al
        /// primer archivo tomado por otro proceso, así que voy ítem por ítem: lo que no se puede
        /// borrar ahora se borra en la próxima corrida, en vez de quedar todo acumulado.
        /// La ruta se resuelve desde el directorio del exe: el CurrentDirectory puede haber
        /// cambiado y con una ruta relativa el borrado apuntaba a cualquier lado.
        /// </summary>
        public void CleanUp()
        {
            String testResults = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");

            if (!Directory.Exists(testResults))
                return;

            foreach (String directory in this.EnumerateOrEmpty(testResults, true))
                this.TryDelete(() => Directory.Delete(directory, true));

            foreach (String file in this.EnumerateOrEmpty(testResults, false))
                this.TryDelete(() => File.Delete(file));

            this.TryDelete(() => Directory.Delete(testResults, true));
        }

        private String[] EnumerateOrEmpty(String path, Boolean directories)
        {
            try
            {
                return directories ? Directory.GetDirectories(path) : Directory.GetFiles(path);
            }
            catch (Exception)
            {
                return new String[0];
            }
        }

        private void TryDelete(Action delete)
        {
            try
            {
                delete();
            }
            catch (Exception)
            {
                // Handle todavía tomado (MSTest recién muerto, antivirus, explorador abierto en la
                // carpeta): lo dejo para la próxima corrida.
            }
        }

        public void SaveSettings()
        {
            Settings.Default.Save();
        }

        #endregion Methods
    }
}