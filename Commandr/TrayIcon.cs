using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;

namespace Commandr
{
    public sealed class TrayIcon : IDisposable
    {
        #region Members

        private MainWindow window;

        private HotKey hotKey;
        private NotifyIcon icon;

        #endregion

        public TrayIcon(MainWindow window)
        {
            this.window = window;

            this.hotKey = new HotKey(Key.Q, Environment.OSVersion.Version.Major == 10 ? KeyModifier.Ctrl : KeyModifier.Win, (key) => this.Show(window));// window.Focus());

            this.icon = new NotifyIcon
            {
                Icon = new Icon(@"Images\Commander.ico"),
                Visible = true
            };

            this.icon.DoubleClick += (o, a) => window.Show();
            this.icon.MouseClick += this.HandleClick;
        }

        private void Show(MainWindow window)
        {
            window.Show();

            window.Activate();
        }

        private void HandleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.window.ShouldClose = true;
                this.window.Close();
            }
            else
                this.window.Show();
        }

        public void Dispose()
        {
            this.icon.Visible = false;
            this.hotKey.Unregister();
            this.icon.Dispose();
            this.hotKey.Dispose();
        }
    }
}