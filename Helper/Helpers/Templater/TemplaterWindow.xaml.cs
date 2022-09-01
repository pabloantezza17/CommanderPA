using System;
using System.Windows;
using MahApps.Metro.Controls;
using Mustache;
using Newtonsoft.Json;

namespace Helper
{
    public partial class TemplaterWindow : MetroWindow
    {
        public TemplaterWindow(String variables)
        {
            this.InitializeComponent();

            this.txtVariables.Text = variables;

            this.txtTemplate.Text = "{{#each this}}\npublic const String {{name}}PropertyName = \"{{name}}\";\n[DataMember]\npublic {{type}} {{name}} {get; set;}\n{{/each}}";

            this.BtnFormatJson_Click(null, null);
        }

        public TemplaterWindow()
            : this("[{\"type\": \"Int64\", \"name\": \"Id\" },{\"type\": \"String\", \"name\": \"Description\" }]")
        {
        }

        private void BtnGenerar_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                FormatCompiler compiler = new FormatCompiler();
                compiler.RemoveNewLines = false;
                Generator generator = compiler.Compile(txtTemplate.Text);
                String result = generator.Render(JsonConvert.DeserializeObject<dynamic>(txtVariables.Text));

                this.txtResult.Text = result;
            }
            catch (Exception ex)
            {
                Framework.Utils.HandleException(ex);
            }
        }

        private void BtnFormatJson_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                this.txtVariables.Text = Framework.Utils.FormatJson(this.txtVariables.Text);
            }
            catch (Exception ex)
            {
                Framework.Utils.HandleException(ex);
            }
        }
    }
}