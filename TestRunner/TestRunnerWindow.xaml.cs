using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TestRunner.Dialogs;

namespace TestRunner
{
    public partial class TestRunnerWindow : Window
    {
        #region Members

        private String UserPath;
        private TesterVM VM;
        private TestManager _testManager;
        private MainWindow mainWindow;

        #endregion

        #region Constructor

        public TestRunnerWindow(MainWindow mainWindow, TesterVM vm)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            this.VM = vm;

            this.DataContext = vm;

            this.UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        #endregion

        #region Properties

        public TestManager Tests
        {
            get
            {
                if (this._testManager == null)
                    this._testManager = new TestManager(this.VM);

                return this._testManager;
            }
        }

        #endregion

        #region Methods

        private void ShowTestInfo(TestEntity test)
        {
            if (test == null)
                return;

            // Copio clase + test al portapapeles y muestro el nombre crudo para poder rastrearlo.
            String classAndTest = test.ClassName + "." + test.TestName;
            this.CopyToClipboard(classAndTest);

            MessageBox.Show(
                test.FullName + "\n\n(Copiado al portapapeles: " + classAndTest + ")",
                test.State.ToString() + ": " + test.Location);
        }

        private void CopyToClipboard(String text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // El portapapeles puede estar ocupado por otro proceso; no interrumpimos por eso.
            }
        }

        private static TestEntity EntityFrom(Object sender)
        {
            return (sender as FrameworkElement)?.DataContext as TestEntity;
        }

        public void RunTests(List<TestView> tests)
        {
            this.VM.CleanUp();

            new Thread(() => this.Tests.RunTests(tests)).Start();
        }

        #endregion

        #region Command

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Tests.KillProcesses();

            Environment.Exit(0);
        }

        /// <summary>
        /// Frena la corrida desde el botón: mata los MSTest en curso y no arranca los assemblies
        /// que faltaban. La ventana queda abierta con los resultados que alcanzaron a llegar.
        /// </summary>
        private void Cancel_Click(Object sender, RoutedEventArgs e)
        {
            if (this.VM.Cancelled)
                return;

            Boolean confirmed = MessageDialog.Confirm(
                "¿Frenar la corrida?",
                "Se matan los MSTest en curso y no se arrancan los assemblies que faltan. Los resultados que ya llegaron quedan en pantalla.",
                "Frenar",
                "Seguir",
                DialogKind.Stopped);

            if (!confirmed)
                return;

            this.VM.RequestCancel();
        }

        private void List_MouseDoubleClick(Object sender, MouseButtonEventArgs e)
        {
            ListBox list = sender as ListBox;
            this.ShowTestInfo(list.SelectedItem as TestEntity);
        }

        private void List_KeyDown(object sender, KeyEventArgs e)
        {
            ListBox list = sender as ListBox;
            if (e.Key == Key.Enter)
                this.ShowTestInfo(list.SelectedItem as TestEntity);
        }

        private void CopyClassAndTest_Click(Object sender, RoutedEventArgs e)
        {
            TestEntity test = EntityFrom(sender);
            if (test != null)
                this.CopyToClipboard(test.ClassName + "." + test.TestName);
        }

        private void CopyClass_Click(Object sender, RoutedEventArgs e)
        {
            TestEntity test = EntityFrom(sender);
            if (test != null)
                this.CopyToClipboard(test.ClassName);
        }

        private void CopyTest_Click(Object sender, RoutedEventArgs e)
        {
            TestEntity test = EntityFrom(sender);
            if (test != null)
                this.CopyToClipboard(test.TestName);
        }

        private void CopyFullName_Click(Object sender, RoutedEventArgs e)
        {
            TestEntity test = EntityFrom(sender);
            if (test != null)
                this.CopyToClipboard(test.FullName);
        }

        #endregion
    }
}