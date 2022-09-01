using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MahApps.Metro.Controls;

namespace Commandr
{
    public partial class ServersStatus : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ServersStatus()
        {
            InitializeComponent();

            this.LoadServers();
        }

        #region Properties

        private ObservableCollection<Server> servers;
        public ObservableCollection<Server> Servers
        {
            get
            {
                return this.servers;
            }
            set
            {
                if (this.servers != value)
                {
                    this.servers = value;

                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Servers"));
                }
            }
        }

        #endregion

        #region Methods

        private void LoadServers()
        {
            this.ServersList.ItemsSource = this.Servers = new ObservableCollection<Server>();

            var serversString = Configuration.Default.Servers;

            var servers = serversString.Split(';').Where(s => !String.IsNullOrEmpty(s));

            var reader = new ServerInfoReader();

            reader.OnServerLoaded += this.Reader_OnServerLoaded;

            reader.Read(servers);
        }

        private void Reader_OnServerLoaded(object sender, Server e)
        {
            this.ServersList.Dispatcher.BeginInvoke(new Action(() => this.Servers.Add(e)));
        }

        #endregion
    }
}