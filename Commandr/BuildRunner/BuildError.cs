using System;
using System.IO;

namespace Commandr.BuildRunner
{
    public class BuildError
    {
        #region Members

        public String File { get; set; }
        public String Message { get; set; }
        public String Raw { get; set; }

        #endregion

        #region Constructor

        public BuildError(String line)
        {
            this.Raw = line;

            // Formato típico de MSBuild:
            //   C:\...\Archivo.cs(12,34): error CS1002: ; expected [C:\...\proyecto.csproj]
            Int32 idx = line.IndexOf(": error", StringComparison.OrdinalIgnoreCase);

            if (idx > 0)
            {
                String left = line.Substring(0, idx);

                // Separo la ruta del "(línea,columna)" para mostrar sólo el nombre del archivo.
                Int32 paren = left.LastIndexOf('(');
                String path = paren > 0 ? left.Substring(0, paren) : left;
                String location = paren > 0 ? left.Substring(paren) : String.Empty;

                this.File = Path.GetFileName(path) + location;

                // Todo lo que sigue a ": " es el mensaje del error.
                this.Message = line.Substring(idx + 2).Trim();

                // Descarto el " [proyecto.csproj]" que MSBuild agrega al final.
                Int32 bracket = this.Message.LastIndexOf(" [", StringComparison.Ordinal);
                if (bracket > 0)
                    this.Message = this.Message.Substring(0, bracket);
            }
            else
            {
                this.File = String.Empty;
                this.Message = line;
            }
        }

        #endregion

        #region Methods

        public override String ToString()
        {
            return this.Raw;
        }

        #endregion
    }
}
