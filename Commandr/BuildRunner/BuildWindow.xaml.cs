using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Commandr.BuildRunner
{
    public partial class BuildWindow : Window
    {
        #region Members

        private readonly BuildVM VM;
        private DispatcherTimer timer;

        #endregion

        #region Constructor

        public BuildWindow(BuildVM vm)
        {
            InitializeComponent();

            this.VM = vm;
            this.DataContext = vm;

            this.VM.PropertyChanged += this.VM_PropertyChanged;

            // Refresca el tiempo transcurrido mientras dura la compilación.
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            this.timer.Tick += (s, e) => this.VM.RaiseTime();
            this.timer.Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Cerrar la ventana cancela la compilación: no tiene sentido seguir ocupando la máquina
        /// (y bloqueando DLLs) por un build que ya nadie va a mirar.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            this.timer.Stop();

            if (this.VM.Status == BuildStatus.Building)
                this.VM.RequestCancel();
        }

        private void VM_PropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Status")
                return;

            if (this.VM.Status != BuildStatus.Building)
                this.timer.Stop();

            // La barra de acento del header refleja el resultado.
            switch (this.VM.Status)
            {
                case BuildStatus.Success:
                    this.AccentBar.Background = (Brush)this.FindResource("GreenBrush");
                    break;
                case BuildStatus.Failed:
                    this.AccentBar.Background = (Brush)this.FindResource("RedBrush");
                    break;
                case BuildStatus.Cancelled:
                    this.AccentBar.Background = (Brush)this.FindResource("AmberBrush");
                    break;
            }
        }

        private void ShowErrorInfo(BuildError error)
        {
            if (error == null)
                return;

            Clipboard.SetText(error.Raw);
            MessageBox.Show(error.Raw, error.File);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Frena la compilación desde el botón del header: mata MSBuild y sus nodos worker,
        /// dejando la ventana abierta para ver lo que alcanzó a reportar.
        /// </summary>
        private void Cancel_Click(Object sender, RoutedEventArgs e)
        {
            if (this.VM.Status != BuildStatus.Building)
                return;

            this.VM.RequestCancel();
        }

        private void ListErrors_MouseDoubleClick(Object sender, MouseButtonEventArgs e)
        {
            ListBox list = sender as ListBox;
            this.ShowErrorInfo(list.SelectedItem as BuildError);
        }

        #endregion
    }
}
