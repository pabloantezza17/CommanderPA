using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Framework;
using Framework.UI;

namespace Helper
{
    public partial class EditarDatosParaTemplater : ObservableWindow
    {
        public EditarDatosParaTemplater(IEnumerable<Match> matches)
        {
            this.InitializeComponent();

            this.Matches = matches;

            this.MakeTemplate(matches);
        }

        private void MakeTemplate(IEnumerable<Match> matches)
        {
            try
            {
                var builder = new StringBuilder();

                builder.AppendLine("{");

                var match = matches.OrderByDescending(m => m.Groups.Count).First();

                for (Int32 i = 1; i < match.Groups.Count; i++)
                    builder.AppendLine("\"group" + i + "\": \"{" + (i - 1) + "}\",");

                builder.AppendLine("}");

                var result = builder.ToString();

                result = Utils.FormatJson(result);

                this.dataTemplateTxt.Text = result;
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);

                this.Close();
            }
        }

        public IEnumerable<Match> Matches { get; }

        private void AbrirTemplater_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                var builder = new StringBuilder();

                builder.AppendLine("[");

                foreach (var match in this.Matches)
                {
                    var data = this.dataTemplateTxt.Text;

                    for (Int32 i = 1; i < match.Groups.Count; i++)
                        data = data.Replace("{" + (i - 1) + "}", match.Groups[i].ToString());

                    builder.AppendLine(data + ",");
                }

                builder.AppendLine("]");

                new TemplaterWindow(builder.ToString()).Show();
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }

            this.Close();
        }
    }
}