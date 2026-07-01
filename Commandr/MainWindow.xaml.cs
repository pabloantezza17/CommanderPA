using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Commandr
{
    public partial class MainWindow : MetroWindow
    {
        #region Members

        private VM vm;
        private TrayIcon trayIcon;

        #endregion

        public Boolean ShouldClose { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            this.trayIcon = new TrayIcon(this);

            this.DataContext = this.vm = new VM();

            //this.CheckForUpdates();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.trayIcon.Dispose();

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = !this.ShouldClose;

            this.Hide();
        }

        protected override void OnGotFocus(System.Windows.RoutedEventArgs e)
        {
            this.SearchBox.Focus();
            this.SearchBox.SelectAll();

            base.OnGotFocus(e);
        }

        private void SearchBox_KeyDown(Object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                this.FocusList();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (this.CommandsList.SelectedItem == null && this.CommandsList.Items.Count > 0)
                    this.CommandsList.SelectedItem = this.CommandsList.Items[0];

                this.DoAction();
                e.Handled = true;
            }
        }

        private void FocusList()
        {
            if (this.CommandsList.Items.Count == 0) return;

            if (this.CommandsList.SelectedIndex < 0)
                this.CommandsList.SelectedIndex = 0;

            this.CommandsList.Focus();

            var item = this.CommandsList.ItemContainerGenerator
                .ContainerFromIndex(this.CommandsList.SelectedIndex) as System.Windows.Controls.ListBoxItem;

            if (item != null)
                item.Focus();
        }

        private void CommandsList_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DoSecondaryAction();
        }

        private void DoSecondaryAction()
        {
            try
            {
                if (this.vm.DoSecundaryAction(CommandsList.SelectedItem as RowCommand))
                    this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoAction()
        {
            try
            {
                if (this.vm.DoAction(CommandsList.SelectedItem as RowCommand))
                    this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CommandsList_MouseDoubleClick(Object sender, MouseButtonEventArgs e)
        {
            this.DoAction();
        }

        private void CommandsList_KeyDown(Object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.DoAction();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!String.IsNullOrEmpty(this.SearchBox.Text))
                {
                    this.SearchBox.Clear();
                    this.SearchBox.Focus();
                }
                else
                {
                    this.Hide();
                }
            }

            base.OnKeyDown(e);
        }

        //private void CheckForUpdates()
        //{
        //    if (Checker.NeedsUpdate())
        //        this.UpdateButton.Visibility = System.Windows.Visibility.Visible;
        //    else
        //        this.UpdateButton.Visibility = System.Windows.Visibility.Collapsed;
        //}

        private void UpdateButton_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Updater.DoUpdate();

                this.trayIcon.Dispose();

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageBranchesButton_Click(Object sender, System.Windows.RoutedEventArgs e)
        {
            var manager = new BranchManager { Owner = this };

            if (manager.ShowDialog() == true)
                this.vm.RefreshBranches();
        }

        private void Settings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}