using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Framework.DataBase;
using Framework.UI;

namespace Helper
{
    public partial class EncontrarParameters : ObservableWindow
    {
        public EncontrarParameters()
        {
            this.InitializeComponent();

            this.DataContext = this;
        }

        public EncontrarParameters SetRama(String rama)
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
            SELECT
	            Parameter.Parameter_Token			AS Token,
	            ParameterValue.ParameterValue_Value AS Value,
	            Parameter.Parameter_Description		AS Description
            FROM fwk.t_cfg_Parameter Parameter
            INNER JOIN fwk.t_cfg_ParameterValue ParameterValue ON ParameterValue.Parameter_ID = Parameter.Parameter_ID
            WHERE Parameter.Parameter_Token like @token;
            ", this.Rama);
            }
        }

        public ObservableCollection<Parameter> Parameters { get; set; }

        public String Rama { get; private set; }

        private void BtnBuscar_Click(Object sender, RoutedEventArgs e)
        {
            String connectionString = "data source=arrosvmsql033;initial catalog={0};integrated security=True;MultipleActiveResultSets=True;";

            DataReader reader = new DataReader(String.Format(connectionString, "Devp_Corretaje"));

            var parameterToken = new KeyValuePair<String, Object>("token", String.IsNullOrEmpty(this.txtParameterToken.Text) ? null : "%" + this.txtParameterToken.Text + "%");

            this.Parameters = new ObservableCollection<Parameter>(reader.Read<Parameter>(this.Query, parameterToken));

            this.RaisePropertyChangedEvent("Parameters");
        }

        public Command CopyTokenCommand
        {
            get
            {
                return new Command(() => this.CopyTokenRow());
            }
        }

        public Command CopyFunctionCommand
        {
            get
            {
                return new Command(() => this.CopyFunctionRow());
            }
        }

        private void CopyTokenRow()
        {
            var parameter = this.SelectedRow as Parameter;

            Clipboard.SetText(parameter.Token);
        }

        private void CopyFunctionRow()
        {
            var parameter = this.SelectedRow as Parameter;

            Clipboard.SetText(String.Format("[fwk].[fn_cfg_Parameter_GetBIGINT]('{0}');", parameter.Token));
        }

        private Object _selectedRow;

        public Object SelectedRow
        {
            get { return this._selectedRow; }
            set
            {
                if (value != this._selectedRow)
                {
                    this._selectedRow = value;

                    this.RaisePropertyChangedEvent("MuestraMenuContextual");
                    this.RaisePropertyChangedEvent("CopyTokenMenuHeader");
                    this.RaisePropertyChangedEvent("CopyFunctionMenuHeader");
                }
            }
        }

        public String CopyTokenMenuHeader
        {
            get
            {
                var selected = this.SelectedRow as Parameter;

                if (selected == null)
                    return String.Empty;

                return String.Format("Copy \"{0}\"", selected.Token);
            }
        }

        public String CopyFunctionMenuHeader
        {
            get
            {
                var selected = this.SelectedRow as Parameter;

                if (selected == null)
                    return String.Empty;

                return String.Format("Copy \"[fwk].[fn_cfg_Parameter_GetBIGINT]('{0}');\"", selected.Token);
            }
        }

        public Visibility MuestraMenuContextual
        {
            get
            {
                return this.SelectedRow == null ? Visibility.Hidden : Visibility.Visible;
            }
        }
    }
}