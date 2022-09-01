using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            MessageBox.Show(test.Description, test.State.ToString() + ": " + test.Location);
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
            IEnumerable<Process> processes = this.Tests.processes.Where(p => p != null && !p.HasExited);

            foreach (Process p in processes)
                if (p != null && !p.HasExited)
                    p.Kill();

            Environment.Exit(0);
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

        #endregion
    }
}