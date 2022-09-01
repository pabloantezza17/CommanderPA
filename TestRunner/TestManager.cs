using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestRunner
{
    public class TestManager
    {
        #region Members

        private TesterVM VM;
        public List<Process> processes;

        #endregion

        #region Constructor

        public TestManager(TesterVM vm)
        {
            this.VM = vm;
        }

        #endregion

        #region Methods

        public void RunTests(List<TestView> tests)
        {
            this.processes = new List<Process>();

            this.VM.Stopwatch.Start();

            this.CambiarOrdenDeEjecucion(tests);

            Parallel.ForEach(
                tests.Where(t => t.IsSelected),
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                t => this.DoTask(t)
                );

            this.VM.Stopwatch.Stop();

            this.KillProcesses();
            this.VM.Finish();
        }

        private void CambiarOrdenDeEjecucion(List<TestView> tests)
        {
            TestView entitiesTestView = tests.Where(t => t.Description.Contains("FyO.Cor.Business.Entities.Tests.Unit.dll")).FirstOrDefault();
            int entitiesTestViewIndex = tests.FindIndex(t => t.Description == entitiesTestView.Description);

            if (entitiesTestView != null && tests.Count() > 1)
            {
                tests.RemoveAt(entitiesTestViewIndex);
                tests.Insert(0, entitiesTestView);
            }
        }

        public void KillProcesses()
        {
            foreach (Process p in this.processes)
                if (!p.HasExited)
                    p.Kill();
        }

        private Process CreateProcess(TestView test)
        {
            Process p = new Process();

            this.processes.Add(p);

            p.StartInfo.FileName = Settings.Default.MSTest;
            p.StartInfo.Arguments = String.Format(MainWindow.programSingleThread, test.Name);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            p.StartInfo.RedirectStandardOutput = true;

            return p;
        }

        private Object DoTask(Object o)
        {
            TestView testView = o as TestView;

            Thread.CurrentThread.IsBackground = true;

            Process process = this.CreateProcess(testView);
            process.Start();

            String line;
            Boolean gettingResults = false;

            try
            {
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (!gettingResults)
                    {
                        if (line.StartsWith("-------"))
                            gettingResults = true;
                    }
                    else
                        gettingResults = this.VM.AddLine(line);
                }

                process.WaitForExit();
            }
            catch (Exception)
            {
                return null;
            }

            return testView;
        }

        #endregion
    }
}