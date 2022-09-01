using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Commandr
{
    public class ServerInfoReader
    {
        public event EventHandler<Server> OnServerLoaded;

        public void Read(IEnumerable<String> servers)
        {
            foreach (var host in servers)
            {
                Task.Run(() => this.ReadServerStatus(host));
            }
        }

        private void ReadServerStatus(String host)
        {
            ManagementPath path = new ManagementPath()
            {
                NamespacePath = @"root\cimv2",
                Server = host
            };

            ManagementScope scope = new ManagementScope(path);
            string condition = "DriveLetter = 'C:'";
            string[] selectedProperties = new string[] { "FreeSpace", "Capacity", "DriveLetter" };
            SelectQuery query = new SelectQuery("Win32_Volume", condition, selectedProperties);

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    ManagementObject volume = results.Cast<ManagementObject>().SingleOrDefault();

                    ulong freeSpace = (ulong)volume.GetPropertyValue("FreeSpace");
                    ulong capacity = (ulong)volume.GetPropertyValue("Capacity");
                    String driveLetter = (String)volume.GetPropertyValue("DriveLetter");

                    this.RunOnServerLoaded(new Server(host) { FreeSpace = freeSpace, Capacity = capacity, DriveLetter = driveLetter });
                }
            }
            catch (Exception)
            {
                this.RunOnServerLoaded(new Server("Invalid Host"));
            }
        }

        private void RunOnServerLoaded(Server server)
        {
            this.OnServerLoaded?.Invoke(this, server);
        }
    }
}