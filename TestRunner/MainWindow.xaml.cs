using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace TestRunner
{
    public partial class MainWindow : Window
    {
        #region Members

        private List<TestView> Tests;
        private String path;
        public const String buildString = " /testcontainer:\"{0}\" ";
        public const String initialString = " /nologo /noisolation";
        public const String programSingleThread = MainWindow.initialString + MainWindow.buildString;

        public String programPath = Settings.Default.MSTest;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            this.cboRama.ItemsSource = new List<String>
            {
                "dev",
                "main",
                "next",
                "live",
                "R4"
            };

            this.cboRama.SelectedIndex = 2;

            this.UpdateTestList();

            String[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
                this.RunTestsInBranch(args[1]);
        }

        #endregion

        #region Methods

        public void RunTestsInBranch(String branch)
        {
            if (!this.cboRama.Items.Contains(branch))
            {
                MessageBox.Show(String.Format("El valor {0} no es valido como rama!", branch), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            this.cboRama.SelectedValue = branch;

            this.RunSelectedTests();
        }

        private void showSettings_Click(object sender, RoutedEventArgs e)
        {
            this.ShowSettings();
        }

        private void ChangeSelection(Boolean value)
        {
            foreach (var test in this.Tests)
                test.IsSelected = value;

            this.Refresh();
        }

        private void Refresh()
        {
            this.testList.Items.Refresh();
        }

        private void RunSelectedTests()
        {
            var vm = new TesterVM();
            vm.Rama = this.cboRama.SelectedValue as String;
            TestRunnerWindow window = new TestRunnerWindow(this, vm);
            this.Hide();
            this.UpdateTestList();
            window.Show();
            window.RunTests(this.Tests);
        }

        private void ShowSettings()
        {
            new ShowSettings().ShowDialog();
        }

        private void UpdateTestList()
        {
            this.path = String.Format(Settings.Default.ProjectPath + @"\{0}\bin", cboRama.SelectedValue);

            this.Tests = new List<TestView>();

            var testFiles = Directory.GetFiles(this.path, "FyO.*.Tests.*.dll", SearchOption.AllDirectories).Where(s => !s.Contains("FyO.Cor.Db"));

            foreach (String file in testFiles)
            {
                Tests.Add(new TestView()
                {
                    IsSelected = true,
                    Name = file
                });
            }

            this.testList.ItemsSource = Tests;
        }

        #endregion

        #region Commands

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeSelection(true);
        }

        private void deselectAll_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeSelection(false);
        }

        private void runTests_Click(object sender, RoutedEventArgs e)
        {
            this.RunSelectedTests();
        }

        private void runAllTests_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeSelection(true);
            this.RunSelectedTests();
        }

        private void cboRama_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.UpdateTestList();
        }

        #endregion
    }
}