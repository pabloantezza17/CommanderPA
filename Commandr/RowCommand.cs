using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commandr
{
    public class RowCommand
    {
        public RowCommand(string image, string name, FileCommand leftClickCommand, FileCommand rightClickCommand = null)
        {
            this.Image = image;
            this.Name = name;
            this.LeftClickCommand = leftClickCommand;
            this.RightClickCommand = rightClickCommand;
        }

        public String Image { get; set; }

        public String Name { get; private set; }

        public FileCommand LeftClickCommand { get; private set; }

        public FileCommand RightClickCommand { get; private set; }

        public override String ToString()
        {
            return this.Name;
        }
    }
}
