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

        public String Category { get; set; }

        /// <summary>Nombre del icono vectorial (PackIconMaterialKind) que muestra la fila.</summary>
        public String IconKind { get; set; }

        public FileCommand LeftClickCommand { get; private set; }

        public FileCommand RightClickCommand { get; private set; }

        public Boolean HasSecondaryAction
        {
            get
            {
                return this.RightClickCommand != null
                    || (this.LeftClickCommand != null && this.LeftClickCommand.CommandType == FileCommandType.Solution);
            }
        }

        public String SecondaryActionHint
        {
            get
            {
                if (this.RightClickCommand != null)
                    return "Click derecho: acción secundaria";

                if (this.LeftClickCommand != null && this.LeftClickCommand.CommandType == FileCommandType.Solution)
                    return "Click derecho: build de la solución";

                return null;
            }
        }

        public override String ToString()
        {
            return this.Name;
        }
    }
}
