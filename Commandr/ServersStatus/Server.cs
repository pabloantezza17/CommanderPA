using System;
using ByteSizeLib;

namespace Commandr
{
    public class Server
    {
        private string host;

        public Server(string host)
        {
            this.host = host;
        }

        public ulong FreeSpace { get; internal set; }
        public ulong Capacity { get; internal set; }
        public string DriveLetter { get; internal set; }

        public Double FreeSpaceInGB
        {
            get
            {
                return ByteSize.FromBytes(this.FreeSpace).GigaBytes;
            }
        }

        public Double CapacityInGB
        {
            get
            {
                return ByteSize.FromBytes(this.Capacity).GigaBytes;
            }
        }

        public Double UsedSpaceInGB
        {
            get
            {
                return this.CapacityInGB - this.FreeSpaceInGB;
            }
        }
        public Double ProportionalUsedSpace
        {
            get
            {
                return (this.UsedSpaceInGB / this.CapacityInGB) * 100;
            }
        }
        public String Host
        {
            get
            {
                return this.host;
            }
        }
    }
}