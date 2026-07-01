using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Commandr.TfsRunner
{
    public partial class TfsWindow : Window
    {
        #region Members

        private readonly TfsVM VM;
        private DispatcherTimer timer;
        private ScrollViewer outputScroll;

        #endregion

        #region Constructor

        public TfsWindow(TfsVM vm)
        {
            InitializeComponent();

            this.VM = vm;
            this.DataContext = vm;

            this.VM.PropertyChanged += this.VM_PropertyChanged;

            // Auto-scroll: mantengo la última línea a la vista a medida que llega la salida.
            ((INotifyCollectionChanged)this.VM.Output).CollectionChanged += this.Output_CollectionChanged;

            // Refresca el tiempo transcurrido mientras dura el get latest.
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            this.timer.Tick += (s, e) => this.VM.RaiseTime();
            this.timer.Start();
        }

        #endregion

        #region Methods

        private void Output_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
                return;

            // Difiero el scroll a después de procesar el cambio para no entrar en carrera con la
            // virtualización del ListBox (causa de "ItemsControl is inconsistent with its items source").
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
            if (e.PropertyName != "ShowWorking")
                return;

            if (this.VM.Status != TfsStatus.Working)
                this.timer.Stop();

            // La barra de acento del header refleja el resultado.
            switch (this.VM.Status)
            {
                case TfsStatus.Success:
                    this.AccentBar.Background = (Brush)this.FindResource("GreenBrush");
                    break;
                case TfsStatus.Failed:
                    this.AccentBar.Background = (Brush)this.FindResource("RedBrush");
                    break;
            }
        }

        #endregion
    }
}
