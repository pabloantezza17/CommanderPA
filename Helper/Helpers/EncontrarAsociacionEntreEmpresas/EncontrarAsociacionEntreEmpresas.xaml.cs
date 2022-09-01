using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Framework.DataBase;
using Framework.UI;

namespace Helper
{
    public partial class EncontrarAsociacionEntreEmpresas : ObservableWindow
    {
        public EncontrarAsociacionEntreEmpresas()
        {
            this.InitializeComponent();

            this.DataContext = this;
        }

        public EncontrarAsociacionEntreEmpresas SetRama(String rama)
        {
            this.Rama = rama;

            return this;
        }

        public String Query
        {
            get
            {
                return String.Format(@"
            USE {0};
            
            DECLARE @vEmpresaActiva BIGINT = [fwk].[fn_cfg_Parameter_GetBIGINT]('Identifier.MAE.EstadoEmpresa.Activo');

            SELECT TOP 500
                Vendedor.RazonSocial_Empresa    As Vendedor,
	            Comprador.RazonSocial_Empresa   As Comprador
            FROM t_mae_AsociacionVendedorComprador  As asoc
                INNER JOIN t_mae_Empresa            As Vendedor     On Vendedor.ID_Empresa = asoc.ID_Empresa_Vendedor
                INNER JOIN t_mae_Empresa            As Comprador    On Comprador.ID_Empresa = asoc.ID_Empresa_Comprador
            WHERE
	        (
                Comprador.ID_EstadoEmpresa = @vEmpresaActiva 
                AND
                EXISTS 
                (
                    SELECT 1
                    FROM dbo.t_mae_SucursalEmpresa                          As se
                    INNER JOIN dbo.t_mae_EstablecimientoSucursalEmpresa		As ese	On se.ID_SucursalEmpresa = ese.ID_SucursalEmpresa
                    WHERE 
                        se.ID_Empresa = Comprador.ID_Empresa                AND
                        ese.EstaActivo_EstablecimientoSucursalEmpresa = 1   AND
                        ese.EsDestino_EstablecimientoSucursalEmpresa = 1
                )
                AND
                (
		            @razonSocialComprador IS NULL
		            OR
		            Comprador.RazonSocial_Empresa Like @razonSocialComprador
                )
	        )
	        AND
	        (
                Vendedor.ID_EstadoEmpresa = @vEmpresaActiva
                AND
                EXISTS 
                (
                    SELECT 1
                    FROM dbo.t_mae_SucursalEmpresa                          As se
                    INNER JOIN dbo.t_mae_EstablecimientoSucursalEmpresa		As ese	On se.ID_SucursalEmpresa = ese.ID_SucursalEmpresa
                    WHERE 
                        se.ID_Empresa = Vendedor.ID_Empresa                 AND
                        ese.EstaActivo_EstablecimientoSucursalEmpresa = 1   AND
                        ese.EsProcedencia_EstablecimientoSucursalEmpresa = 1
                )
                AND
                (
		            @razonSocialVendedor IS NULL
		            OR
		            Vendedor.RazonSocial_Empresa Like @razonSocialVendedor
                )
	        )
            Order By Comprador.RazonSocial_Empresa, Vendedor.RazonSocial_Empresa;", this.Rama);
            }
        }

        public ObservableCollection<AsociacionEntreEmpresas> AsociacionesEntreEmpresas { get; set; }

        public String Rama { get; private set; }

        private void btnBuscar_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            String connectionString = "data source=arrosvmsql033;initial catalog={0};integrated security=True;MultipleActiveResultSets=True;";

            DataReader reader = new DataReader(String.Format(connectionString, "Devp_Corretaje"));

            var parameterRazonSocialComprador = new KeyValuePair<String, Object>("razonSocialComprador", String.IsNullOrEmpty(this.txtRazonSocialComprador.Text) ? null : "%" + this.txtRazonSocialComprador.Text + "%");
            var parameterRazonSocialVendedor = new KeyValuePair<String, Object>("razonSocialVendedor", String.IsNullOrEmpty(this.txtRazonSocialVendedor.Text) ? null : "%" + this.txtRazonSocialVendedor.Text + "%");

            this.AsociacionesEntreEmpresas = new ObservableCollection<AsociacionEntreEmpresas>(reader.Read<AsociacionEntreEmpresas>(this.Query, parameterRazonSocialComprador, parameterRazonSocialVendedor));

            this.RaisePropertyChangedEvent("AsociacionesEntreEmpresas");
        }
    }
}