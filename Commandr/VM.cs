using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Commandr.Properties;

namespace Commandr
{
    public enum FileCommandType
    {
        Bat, Solution, ExecutableWithArguments, BatWithArguments,
        Executable, Status
    }

    public class VM
    {
        public static String BasePath = Configuration.Default.ProjectPath + @"\{0}";
        private String imageSolution = "/Commandr;component/Images/Solution.png";
        private String imageBatFile = "/Commandr;component/Images/BatFile.ico";
        private String imageExecutable = "/Commandr;component/Images/executable.ico";
        private String imageStatus = "/Commandr;component/Images/status.png";

        private RowCommand NewFileAction(String title, String command, FileCommandType fileCommandType)
        {
            var action = new FileCommand(command, fileCommandType);

            var image = this.DecideIcon(fileCommandType);

            var row = new RowCommand(image, title, action);

            return row;
        }

        private RowCommand NewFileAction(String title, Action actionToExecute, FileCommandType fileCommandType)
        {
            var action = new FileCommand(actionToExecute, fileCommandType);

            var image = this.DecideIcon(fileCommandType);

            var row = new RowCommand(image, title, action);

            return row;
        }

        private RowCommand NewFileAction(String title, String primaryCommand, FileCommandType primaryFileCommandType, String secondaryCommand, FileCommandType secondaryFileCommandType)
        {
            var primaryAction = new FileCommand(primaryCommand, primaryFileCommandType);

            var secondaryAction = new FileCommand(secondaryCommand, secondaryFileCommandType);

            var image = this.DecideIcon(primaryFileCommandType);

            var row = new RowCommand(image, title, primaryAction, secondaryAction);

            return row;
        }

        private RowCommand NewFileAction(String title, Action primaryActionToExecute, FileCommandType primaryFileCommandType, String secondaryCommand, FileCommandType secondaryFileCommandType)
        {
            var primaryAction = new FileCommand(primaryActionToExecute, primaryFileCommandType);

            var secondaryAction = new FileCommand(secondaryCommand, secondaryFileCommandType);

            var image = this.DecideIcon(primaryFileCommandType);

            var row = new RowCommand(image, title, primaryAction, secondaryAction);

            return row;
        }

        private String DecideIcon(FileCommandType fileCommandType)
        {
            switch (fileCommandType)
            {
                case FileCommandType.Bat:
                    return imageBatFile;

                case FileCommandType.Solution:
                    return imageSolution;

                case FileCommandType.ExecutableWithArguments:
                case FileCommandType.BatWithArguments:
                case FileCommandType.Executable:
                    return imageExecutable;

                case FileCommandType.Status:
                    return imageStatus;

                default:
                    throw new NotImplementedException();
            }
        }

        public ObservableCollection<RowCommand> Actions
        {
            get
            {
                return new ObservableCollection<RowCommand>
                {
                    NewFileAction("Build", BasePath + @"\src\build_all_vs2017.bat", FileCommandType.Bat,
                        Directory.GetCurrentDirectory() + @"\TestRunner\build_and_test_vs2017.bat", FileCommandType.BatWithArguments),
                    NewFileAction("Get Latest", () => this.GetLatestVersion(), FileCommandType.Bat,
                        Directory.GetCurrentDirectory() + @"\TestRunner\update_build_and_test_vs2017.bat", FileCommandType.BatWithArguments),
                    NewFileAction("Db", BasePath + @"\src\Db\FyO.Db.sln", FileCommandType.Solution ),
                    NewFileAction("Fwk", BasePath + @"\src\Fwk\Neoris.FWK.sln",  FileCommandType.Solution),
                    NewFileAction("Apc", BasePath + @"\src\FyO.Cor\FyO.Cor.Apc.sln",FileCommandType.Solution),
                    NewFileAction("Apl", BasePath + @"\src\FyO.Cor\FyO.Cor.Apl.sln",FileCommandType.Solution),
                    NewFileAction("Con", BasePath + @"\src\FyO.Cor\FyO.Cor.Con.sln",FileCommandType.Solution),
                    NewFileAction("Doc", BasePath + @"\src\FyO.Cor\FyO.Cor.Doc.sln",FileCommandType.Solution),
                    NewFileAction("Eai", BasePath + @"\src\FyO.Cor\FyO.Cor.Eai.sln",FileCommandType.Solution),
                    NewFileAction("Fac", BasePath + @"\src\FyO.Cor\FyO.Cor.Fac.sln",FileCommandType.Solution),
                    NewFileAction("Int", BasePath + @"\src\FyO.Cor\FyO.Cor.Int.sln",FileCommandType.Solution),
                    NewFileAction("Log", BasePath + @"\src\FyO.Cor\FyO.Cor.Log.sln",FileCommandType.Solution),
                    NewFileAction("Mae", BasePath + @"\src\FyO.Cor\FyO.Cor.Mae.sln",FileCommandType.Solution),
                    NewFileAction("Rie", BasePath + @"\src\FyO.Cor\FyO.Cor.Rie.sln",FileCommandType.Solution),
                    NewFileAction("AppCore", BasePath + @"\src\AppCore\FyO.AppCore.sln",FileCommandType.Solution),
                    NewFileAction("Change DB", () => this.ChangeDB(),FileCommandType.Bat),
                    NewFileAction("Change IIS", BasePath + @"\src\change_iis_branch.bat",FileCommandType.Bat),
                    NewFileAction("Start Scheduler", BasePath + @"\src\FyO.Cor\FyO.Cor.Tasks.Host.WinService\Start.bat",FileCommandType.Bat),
                    NewFileAction("Stop Scheduler", BasePath + @"\src\FyO.Cor\FyO.Cor.Tasks.Host.WinService\Stop.bat",FileCommandType.Bat),
                    NewFileAction("TestRunner", Directory.GetCurrentDirectory() + @"\TestRunner\TestRunner.exe", FileCommandType.ExecutableWithArguments),
                    NewFileAction("Reset IIS", @"iisreset", FileCommandType.Executable),
                    NewFileAction("Helper", Directory.GetCurrentDirectory() + @"\Helper.exe", FileCommandType.ExecutableWithArguments),
                    NewFileAction("Shell", BasePath + @"\src\FyO.Cor\FyO.Cor.UI.Desktop.Shell\bin\Debug\FyO.Cor.UI.Desktop.Shell.exe", FileCommandType.Executable)
                };
            }
        }

        private void RunAsVS2017()
        {
            CommandsTools commands = new CommandsTools();

            commands.RunAsVS2017();
        }

        private void RunAsVS2019()
        {
            CommandsTools commands = new CommandsTools();

            commands.RunAsVS2019();
        }

        private void RunAsSQL()
        {
            CommandsTools commands = new CommandsTools();

            commands.RunAsSQL();
        }

        private void GetLatestVersion()
        {
            TfsTools tfsTools = new TfsTools();

            tfsTools.GetLatestVersion();
        }

        private void ChangeDB()
        {
            var dbChanger = new DataBaseChanger.DBChanger().SetBranch(this.CurrentBranch);

            dbChanger.Show();
        }

        private void ServerStatus()
        {
            var serversStatus = new ServersStatus();

            serversStatus.Show();
        }

        public IEnumerable<String> Branches
        {
            get
            {
                return new[] { "dev", "live", "main", "next", "R4" };
            }
        }

        public String CurrentBranch
        {
            get
            {
                return Settings.Default.CurrentBranch;
            }
            set
            {
                if (Settings.Default.CurrentBranch != value)
                {
                    Settings.Default.CurrentBranch = value;

                    Settings.Default.Save();
                }
            }
        }

        public Boolean DoAction(RowCommand rowCommand)
        {
            if (rowCommand != null && rowCommand.LeftClickCommand != null)
            {
                var fileCommand = rowCommand.LeftClickCommand;

                return DoAction(fileCommand);
            }

            return false;
        }

        public Boolean DoAction(FileCommand fileCommand)
        {
            if (fileCommand != null)
            {
                if (!String.IsNullOrEmpty(fileCommand.Command))
                {
                    ProcessStartInfo info = new ProcessStartInfo();

                    var command = info.FileName = String.Format(fileCommand.Command, this.CurrentBranch);

                    info.WorkingDirectory = Path.GetDirectoryName(command);

                    if (fileCommand.CommandType == FileCommandType.ExecutableWithArguments)
                        info.Arguments = this.CurrentBranch;

                    if (fileCommand.CommandType == FileCommandType.BatWithArguments)
                        info.Arguments = this.SetArguments(String.Format(BasePath, this.CurrentBranch), Directory.GetCurrentDirectory(), this.CurrentBranch);

                    Process.Start(info);

                    return true;
                }
                else if (fileCommand.ActionCommand != null)
                {
                    fileCommand.ActionCommand();

                    return true;
                }
            }

            return false;
        }

        public Boolean DoSecundaryAction(RowCommand rowCommand)
        {
            if (rowCommand == null) return false;

            if (rowCommand.RightClickCommand == null && rowCommand.LeftClickCommand != null && rowCommand.LeftClickCommand.CommandType == FileCommandType.Solution)
            {
                var fileCommand = rowCommand.LeftClickCommand;

                Builder builder = new Builder();

                builder.Build(fileCommand, rowCommand.Name, this.CurrentBranch);

                return true;
            }

            if (rowCommand.RightClickCommand != null)
            {
                var fileCommand = rowCommand.RightClickCommand;

                return DoAction(fileCommand);
            }

            return false;
        }

        public String SetArguments(params String[] args)
        {
            return String.Join(" ", args);
        }
    }
}