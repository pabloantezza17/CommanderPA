using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Framework.DataBase;
using Framework.Entities;
using MahApps.Metro.Controls;

namespace MailSpy
{
    public partial class ShowBody : MetroWindow
    {
        private const String loadAttachmentsScriptFileName = ".\\Scripts\\LoadAttachments.sql";

        public ShowBody(String dbName, MailView mailView)
        {
            this.InitializeComponent();
            this.Texty.Text = mailView.Body;
            this.Webby.NavigateToString(mailView.Body);

            this.AttachmentsList.ItemsSource = this.LoadAttachments(dbName, mailView.Id);

            this.AttachmentsList.MouseDoubleClick += (s, e) =>
            {
                try
                {
                    Process.Start(this.AttachmentsList.SelectedValue as String);
                }
                catch (Exception ex)
                {
                    Framework.Utils.HandleException(ex);
                }
            };
        }

        private IEnumerable<String> LoadAttachments(String dbName, Int64 id)
        {
            var reader = new DataReader(Configuration.Default.ConnectionString);

            var script = String.Format(File.ReadAllText(loadAttachmentsScriptFileName), dbName);

            return reader.Read<ValueOf<String>>(script, new KeyValuePair<String, Object>("id", id)).Select(v => v.Value);
        }

        private void Window_PreviewKeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
            else if (e.Key == Key.Tab)
            {
                e.Handled = true;
                this.TabContainer.SelectedIndex = (this.TabContainer.SelectedIndex + 1) % 2;
            }
        }
    }
}