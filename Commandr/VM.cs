using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using Commandr.Properties;
using Framework.UI;

namespace Commandr
{
    public enum FileCommandType
    {
        Bat, Solution, ExecutableWithArguments, BatWithArguments,
        Executable, Status
    }

    public class VM : ViewModelBase
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

        private RowCommand NewFileAction(String title, String primaryCommand, FileCommandType primaryFileCommandType, Action secondaryActionToExecute, FileCommandType secondaryFileCommandType)
        {
            var primaryAction = new FileCommand(primaryCommand, primaryFileCommandType);

            var secondaryAction = new FileCommand(secondaryActionToExecute, secondaryFileCommandType);

            var image = this.DecideIcon(primaryFileCommandType);

            var row = new RowCommand(image, title, primaryAction, secondaryAction);

            return row;
        }

        private RowCommand NewFileAction(String title, Action primaryActionToExecute, FileCommandType primaryFileCommandType, Action secondaryActionToExecute, FileCommandType secondaryFileCommandType)
        {
            var primaryAction = new FileCommand(primaryActionToExecute, primaryFileCommandType);

            var secondaryAction = new FileCommand(secondaryActionToExecute, secondaryFileCommandType);

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

        public VM()
        {
            this.allActions = this.BuildActions();

            this.actionsView = CollectionViewSource.GetDefaultView(this.allActions);
            this.actionsView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            this.actionsView.Filter = this.MatchesSearch;
        }

        private readonly ObservableCollection<RowCommand> allActions;
        private readonly ICollectionView actionsView;
        private String searchText;

        /// <summary>Vista filtrable y agrupada por categoría que consume la UI.</summary>
        public ICollectionView ActionsView
        {
            get { return this.actionsView; }
        }

        /// <summary>Texto del buscador; al cambiar refresca el filtro de <see cref="ActionsView"/>.</summary>
        public String SearchText
        {
            get { return this.searchText; }
            set
            {
                if (this.searchText != value)
                {
                    this.searchText = value;
                    this.actionsView.Refresh();
                    this.RaisePropertyChangedEvent(nameof(SearchText));
                }
            }
        }

        private Boolean MatchesSearch(Object item)
        {
            if (String.IsNullOrWhiteSpace(this.searchText)) return true;

            var row = item as RowCommand;

            return row != null && row.Name.IndexOf(this.searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private RowCommand Make(String category, String iconKind, RowCommand row)
        {
            row.Category = category;
            row.IconKind = iconKind;

            return row;
        }

        private ObservableCollection<RowCommand> BuildActions()
        {
            const String scripts = "Scripts";
            const String solutions = "Soluciones";
            const String tools = "Herramientas";

            return new ObservableCollection<RowCommand>
            {
                Make(scripts, "Cogs", NewFileAction("Build", BasePath + @"\src\build_all_vs2017.bat", FileCommandType.Bat,
                    (Action)(() => this.BuildAndTest()), FileCommandType.BatWithArguments)),
                Make(scripts, "Sync", NewFileAction("Get Latest", () => this.GetLatestVersion(), FileCommandType.Bat,
                    (Action)(() => this.UpdateBuildAndTest()), FileCommandType.BatWithArguments)),
                Make(scripts, "Database", NewFileAction("Change DB", () => this.ChangeDB(),FileCommandType.Bat)),
                Make(scripts, "Web", NewFileAction("Change IIS", BasePath + @"\src\change_iis_branch.bat",FileCommandType.Bat)),
                Make(scripts, "Play", NewFileAction("Start Scheduler", BasePath + @"\src\FyO.Cor\FyO.Cor.Tasks.Host.WinService\Start.bat",FileCommandType.Bat)),
                Make(scripts, "Stop", NewFileAction("Stop Scheduler", BasePath + @"\src\FyO.Cor\FyO.Cor.Tasks.Host.WinService\Stop.bat",FileCommandType.Bat)),
                Make(scripts, "Restart", NewFileAction("Reset IIS", @"iisreset", FileCommandType.Executable)),

                Make(solutions, "VisualStudio", NewFileAction("Db", BasePath + @"\src\Db\FyO.Db.sln", FileCommandType.Solution )),
                Make(solutions, "VisualStudio", NewFileAction("Fwk", BasePath + @"\src\Fwk\Neoris.FWK.sln",  FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Apc", BasePath + @"\src\FyO.Cor\FyO.Cor.Apc.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Apl", BasePath + @"\src\FyO.Cor\FyO.Cor.Apl.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Con", BasePath + @"\src\FyO.Cor\FyO.Cor.Con.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Doc", BasePath + @"\src\FyO.Cor\FyO.Cor.Doc.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Eai", BasePath + @"\src\FyO.Cor\FyO.Cor.Eai.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Fac", BasePath + @"\src\FyO.Cor\FyO.Cor.Fac.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Int", BasePath + @"\src\FyO.Cor\FyO.Cor.Int.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Log", BasePath + @"\src\FyO.Cor\FyO.Cor.Log.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Mae", BasePath + @"\src\FyO.Cor\FyO.Cor.Mae.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("Rie", BasePath + @"\src\FyO.Cor\FyO.Cor.Rie.sln",FileCommandType.Solution)),
                Make(solutions, "VisualStudio", NewFileAction("AppCore", BasePath + @"\src\AppCore\FyO.AppCore.sln",FileCommandType.Solution)),

                Make(tools, "TestTube", NewFileAction("TestRunner", Directory.GetCurrentDirectory() + @"\TestRunner\TestRunner.exe", FileCommandType.ExecutableWithArguments)),
                Make(tools, "Toolbox", NewFileAction("Helper", Directory.GetCurrentDirectory() + @"\Helper.exe", FileCommandType.ExecutableWithArguments)),
                Make(tools, "Application", NewFileAction("Shell", BasePath + @"\src\FyO.Cor\FyO.Cor.UI.Desktop.Shell\bin\Debug\FyO.Cor.UI.Desktop.Shell.exe", FileCommandType.Executable))
            };
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

            tfsTools.GetLatestVersion(this.BranchesToGet(this.CurrentBranch));
        }

        /// <summary>Click derecho en "Get Latest": get latest de todas las ramas + build + tests.</summary>
        private void UpdateBuildAndTest()
        {
            this.RunUpdatePipeline(includeGetLatest: true);
        }

        /// <summary>Click derecho en "Build": build de todas las soluciones + tests (sin get latest).</summary>
        private void BuildAndTest()
        {
            this.RunUpdatePipeline(includeGetLatest: false);
        }

        private void RunUpdatePipeline(Boolean includeGetLatest)
        {
            var vm = new UpdateRunner.UpdateVM { Branch = this.CurrentBranch };

            var branches = includeGetLatest
                ? this.BranchesToGet(AllBranchesLabel)
                : Enumerable.Empty<String>();

            var window = new UpdateRunner.UpdateWindow(
                vm,
                branches,
                this.CurrentBranch,
                String.Format(BasePath, this.CurrentBranch),
                Directory.GetCurrentDirectory(),
                includeGetLatest);

            window.Show();
            window.Run();
        }

        /// <summary>Etiqueta especial del selector que baja todas las ramas configuradas.</summary>
        public const String AllBranchesLabel = "Todas";

        /// <summary>Ramas configuradas en Settings (lista separada por comas), sin la opción "Todas".</summary>
        private IEnumerable<String> ConfiguredBranches
        {
            get
            {
                return (Settings.Default.Branches ?? String.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => b.Trim())
                    .Where(b => b.Length > 0);
            }
        }

        /// <summary>
        /// Decide qué ramas bajar según lo elegido en el selector: "Todas" (o vacío) baja
        /// todas las configuradas; cualquier otro valor —incluida una rama tipeada a mano—
        /// baja solo esa. Normaliza la primera letra a mayúscula para que matchee la ruta TFS.
        /// </summary>
        private IEnumerable<String> BranchesToGet(String selected)
        {
            var chosen = String.IsNullOrWhiteSpace(selected) || selected == AllBranchesLabel
                ? this.ConfiguredBranches
                : new[] { selected };

            return chosen.Select(NormalizeBranch).ToList();
        }

        private static String NormalizeBranch(String branch)
        {
            branch = (branch ?? String.Empty).Trim();

            if (branch.Length == 0) return branch;

            return Char.ToUpperInvariant(branch[0]) + branch.Substring(1);
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
                return this.ConfiguredBranches.ToList();
            }
        }

        /// <summary>Vuelve a leer la lista de branches del setting y refresca el combo de la UI.</summary>
        public void RefreshBranches()
        {
            this.RaisePropertyChangedEvent(nameof(Branches));
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

                    this.RaisePropertyChangedEvent(nameof(CurrentBranch));
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