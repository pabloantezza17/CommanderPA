using System;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace MailSpy
{
    public partial class MailSpyWindow : MetroWindow
    {
        public MailSpyWindow()
        {
            this.InitializeComponent();

            this.DataContext = new MailSpyWindowVM();

            this.MailsDataGrid.SelectionChanged += (s, e) =>
            {
                if (e.OriginalSource is ComboBox)
                    return;

                this.ViewModel.SelectItem(e.AddedItems);
            };

            this.MailsDataGrid.MouseDoubleClick += (s, e) => this.ViewModel.ShowSelected();

            this.MailsDataGrid.PreviewKeyDown += this.HandleKeyDown;

            this.ViewModel.Initialize();
        }

        private void HandleKeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                this.ViewModel.ShowSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
                this.Close();
        }

        public MailSpyWindowVM ViewModel { get { return this.DataContext as MailSpyWindowVM; } }
    }
}