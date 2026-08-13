using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Commandr.ScriptRunner
{
    public partial class ScriptWindow : Window
    {
        #region Members

        private readonly ScriptVM VM;
        private readonly ScriptExecutor executor;
        private DispatcherTimer timer;
        private ScrollViewer outputScroll;

        #endregion

        #region Constructor

        /// <param name="title">Título mostrado en el header (ej. "Change IIS - Develop").</param>
        /// <param name="path">Ruta al .bat o al ejecutable (o nombre en PATH).</param>
        /// <param name="isBatch">True si <paramref name="path"/> es un .bat.</param>
        /// <param name="successText">Texto de estado al terminar bien.</param>
        public ScriptWindow(String title, String path, Boolean isBatch, String successText)
        {
            InitializeComponent();

            this.VM = new ScriptVM(title);
            this.DataContext = this.VM;

            this.executor = new ScriptExecutor(this.VM, path, isBatch, successText);

            this.VM.PropertyChanged += this.VM_PropertyChanged;

            // Auto-scroll: mantengo la última línea a la vista a medida que llega la salida.
            ((INotifyCollectionChanged)this.VM.Output).CollectionChanged += this.Output_CollectionChanged;

            // Refresca el tiempo transcurrido mientras dura el proceso.
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            this.timer.Tick += (s, e) => this.VM.RaiseTime();
            this.timer.Start();
        }

        #endregion

        #region Methods

        public void Run()
        {
            new Thread(() => this.executor.Run()) { IsBackground = true }.Start();
        }

        private void Close_Click(Object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Output_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
                return;

            // Difiero el scroll para no entrar en carrera con la virtualización del ListBox.
            this.Dispatcher.BeginInvoke(new Action(this.ScrollOutputToEnd), DispatcherPriority.Background);
        }

        private void ScrollOutputToEnd()
        {
            try
            {
                if (this.outputScroll == null)
                    this.outputScroll = FindScrollViewer(this.OutputList);

                this.outputScroll?.ScrollToEnd();
            }
            catch (Exception)
            {
                // El auto-scroll es cosmético: si falla, no interrumpimos el proceso.
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer viewer)
                return viewer;

            Int32 count = VisualTreeHelper.GetChildrenCount(root);
            for (Int32 i = 0; i < count; i++)
            {
                ScrollViewer found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private void VM_PropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Status")
                return;

            if (this.VM.Status != ScriptStatus.Working)
                this.timer.Stop();

            // La barra de acento del header refleja el resultado.
            switch (this.VM.Status)
            {
                case ScriptStatus.Success:
                    this.AccentBar.Background = (Brush)this.FindResource("GreenBrush");
                    break;
                case ScriptStatus.Failed:
                    this.AccentBar.Background = (Brush)this.FindResource("RedBrush");
                    break;
            }
        }

        #endregion
    }
}
