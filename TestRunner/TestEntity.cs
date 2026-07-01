using System;

namespace TestRunner
{
    #region Enum

    public enum TestState
    {
        Failed,
        Passed,
        Inconclusive,
        Integration
    }

    #endregion

    public class TestEntity
    {
        #region Members

        public TestState State { get; set; }
        public String Description { get; set; }
        public String Location { get; set; }

        // Nombres crudos (sin el formateo de Description) para poder rastrear el test en la solución.
        public String FullName { get; set; }
        public String ClassName { get; set; }
        public String TestName { get; set; }

        #endregion

        #region Constructor

        public TestEntity(String info)
        {
            String[] parts = info.Split(new String[] {
                " ",
                ".Tests.Unit.",
                ".Tests."
            }, StringSplitOptions.RemoveEmptyEntries);

            #region Switch

            switch (parts.Length > 0 ? parts[0] : String.Empty)
            {
                case "Failed":
                    this.State = TestState.Failed;
                    break;

                case "Passed":
                    this.State = TestState.Passed;
                    break;

                default:
                    this.State = TestState.Inconclusive;
                    break;
            }

            #endregion

            if (info.Contains("Integration"))
                this.State = TestState.Integration;

            // Robusto ante líneas cuyo FQN no contiene ".Tests." (menos de 3 partes),
            // que ahora pueden llegar al rutear también los Passed de FWK/Integración.
            this.Location = parts.Length > 1 ? parts[1] : String.Empty;

            String descripcion = parts.Length > 2 ? parts[2] : (parts.Length > 1 ? parts[1] : info);

            // Guardo el FQN crudo (clase + método, tal cual lo reporta MSTest) antes de formatearlo,
            // así se puede copiar y buscar directamente en la solución.
            this.FullName = descripcion;

            Int32 lastDot = descripcion.LastIndexOf('.');
            if (lastDot > 0)
            {
                this.ClassName = descripcion.Substring(0, lastDot);
                this.TestName = descripcion.Substring(lastDot + 1);
            }
            else
            {
                this.ClassName = descripcion;
                this.TestName = descripcion;
            }

            // Para clases anidadas ("Externa+Interna") me quedo con el nombre simple de la clase.
            Int32 plus = this.ClassName.LastIndexOf('+');
            if (plus >= 0)
                this.ClassName = this.ClassName.Substring(plus + 1);

            this.Description = descripcion.Replace("+", ":\n").Replace('_', ' ').Replace(".", ": ");
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return this.Description;
        }

        #endregion
    }
}