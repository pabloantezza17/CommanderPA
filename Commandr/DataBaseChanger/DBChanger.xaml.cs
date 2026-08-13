using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Framework.DataBase;

namespace Commandr.DataBaseChanger
{
    public partial class DBChanger : Window
    {
        private ICollectionView databasesView;

        public DBChanger()
        {
            InitializeComponent();

            this.DataBases = this.LoadDataBases();

            this.databasesView = CollectionViewSource.GetDefaultView(this.DataBases);
            this.databasesView.Filter = this.MatchesSearch;

            this.DataBaseList.ItemsSource = this.databasesView;
        }

        public DBChanger SetBranch(String branch)
        {
            this.Branch = branch;

            this.BranchLabel.Text = branch;

            this.DataBaseList.SelectedIndex = 0;

            return this;
        }

        private Boolean MatchesSearch(Object item)
        {
            var search = this.SearchBox.Text;

            if (String.IsNullOrWhiteSpace(search)) return true;

            var name = item as String;

            return name != null && name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SearchBox_TextChanged(Object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.databasesView?.Refresh();

            if (this.DataBaseList.SelectedIndex < 0 && this.DataBaseList.Items.Count > 0)
                this.DataBaseList.SelectedIndex = 0;
        }

        #region Properties

        public String Query
        {
            get
            {
                return @"
                         SELECT db.name AS Value
                         FROM master.dbo.sysdatabases db
                         WHERE db.name NOT IN ('master', 'tempdb', 'model', 'msdb', 'DBA') and db.name like '%fyo%';
                        ";
            }
        }

        public List<String> DataBases { get; set; }

        public IEnumerable<String> ArchivosConfig
        {
            get
            {
                return new[]
                {
                    Configuration.Default.ProjectPath + @"\{0}\src\AppCore\FyO.Sec.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\Fwk\Neoris.Fwk.EX.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\Fwk\Neoris.Fwk.SEC.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\Fwk\Neoris.Fwk.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\Fwk\Neoris.Fwk.Tasks.Host.WinService\bin\Debug\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Apc.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Con.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Mae.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Rie.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Tasks.Host.WinService\bin\Debug\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Apl.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Doc.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Fac.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Eai.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Log.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\src\FyO.Cor\FyO.Cor.Int.Services.Host.IIS\ConnectionStrings.config",
                    Configuration.Default.ProjectPath + @"\{0}\bin\ConnectionStrings.config"
                }.Select(a => String.Format(a, this.Branch));
            }
        }

        public String SelectedDataBase { get { return this.DataBaseList.SelectedItem as String; } }

        public String Branch { get; private set; }

        #endregion

        #region Methods

        private List<String> LoadDataBases()
        {
            var reader = new DataReader(Configuration.Default.InitialConnectionString);

            var values = reader.Read<ValueOf<String>>(this.Query);

            return values.Select(v => v.Value).ToList();
        }

        private void DataBaseList_MouseDoubleClick(Object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.ChangeDB(@"4P020\SQL19", this.SelectedDataBase);
        }

        private void ChangeDB(String hostname, String catalog, Boolean removeUser = false)
        {
            try
            {
                var connectionStrings = File.ReadAllText("ConnectionStrings.xml");

                connectionStrings = connectionStrings.Replace("HostNameDataBase", hostname);

                connectionStrings = connectionStrings.Replace("NombreBaseDatos", catalog);

                if (removeUser)
                    connectionStrings = connectionStrings.Replace("User ID=fyo_corretaje; Password=Corret@je;", "Integrated Security=true;");

                foreach (var archivo in this.ArchivosConfig)
                {
                    try
                    {
                        File.WriteAllText(archivo, connectionStrings);
                    }
                    catch (System.IO.DirectoryNotFoundException)
                    {
                        throw new System.IO.DirectoryNotFoundException(String.Format("No se puede hallar la ruta: {0}", archivo));
                    }
                }

                MessageBox.Show(String.Format("Changed {0} ConnectionString.config files", this.ArchivosConfig.Count()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.Close();
            }
        }

        private void DataBaseList_KeyDown(Object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                this.ChangeDB(@"SERVER21\SQL2014ENT", this.SelectedDataBase);
        }

        private void CustomDB_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            String hostname = "localhost";
            String catalog = "local_Corretaje";

            if (InputBox.Show("ConnectionString", "Input the custom hostname:", ref hostname) == System.Windows.Forms.DialogResult.OK
                && InputBox.Show("ConnectionString", "Input the database name:", ref catalog) == System.Windows.Forms.DialogResult.OK)
                this.ChangeDB(hostname, catalog, true);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            this.DataBaseList.Focus();
        }

        #endregion
    }
}