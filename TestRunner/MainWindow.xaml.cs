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
        public const String settingsString = " /testsettings:\"{1}\" ";
        public const String programSingleThread = MainWindow.initialString + MainWindow.settingsString + MainWindow.buildString;
        public const String testSettingsFile = "Fast.testsettings";

        public String programPath = Settings.Default.MSTest;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
             
            this.cboRama.ItemsSource = new List<String>
            {
                "dev",
                "live",
                "R18"
            };

            this.cboRama.SelectedIndex = 0;

            this.UpdateTestList();

            String[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
                this.RunTestsInBranch(args[1]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Ruta del .testsettings que apaga el deployment de MSTest. Sin esto cada assembly copia
        /// todo su bin (~62 MB medidos) a TestResults antes de arrancar; con ~50 assemblies son
        /// varios GB de I/O por corrida. Ningún test del repo usa [DeploymentItem], así que correr
        /// desde el bin directo da el mismo resultado.
        /// </summary>
        public static String TestSettingsPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MainWindow.testSettingsFile); }
        }

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
            this.path = String.Format(Settings.Default.ProjectPath + @"\{0}", cboRama.SelectedValue);

            this.Tests = new List<TestView>();

            var testFiles = Directory.GetFiles(this.path, "*.Tests*.dll", SearchOption.AllDirectories)
                .Where(s => !s.Contains(@"\obj\"))
                .Where(s => !s.Contains("FyO.Cor.Db"))
                .Where(s => (Path.GetFileName(s).StartsWith("FyO.") || Path.GetFileName(s).StartsWith("Neoris."))
                            && (Path.GetFileName(s).EndsWith(".Tests.Unit.dll") || Path.GetFileName(s).EndsWith(".Tests.Integration.dll")))
                .GroupBy(s => Path.GetFileName(s))
                .Select(g => g.OrderBy(s => s.Contains(@"\src\") ? 1 : 0).First());

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