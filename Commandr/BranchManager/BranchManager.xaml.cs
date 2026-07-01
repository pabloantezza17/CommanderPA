using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Commandr.Properties;
using MahApps.Metro.Controls;

namespace Commandr
{
    public partial class BranchManager : MetroWindow
    {
        private readonly ObservableCollection<String> branches;

        public BranchManager()
        {
            InitializeComponent();

            this.branches = new ObservableCollection<String>(
                (Settings.Default.Branches ?? String.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => b.Trim())
                    .Where(b => b.Length > 0));

            this.DataContext = this.branches;
        }

        private void AddBranch()
        {
            var name = (this.NewBranchBox.Text ?? String.Empty).Trim();

            if (name.Length == 0) return;

            if (this.branches.Any(b => String.Equals(b, name, StringComparison.OrdinalIgnoreCase)))
            {
                this.NewBranchBox.Clear();
                return;
            }

            this.branches.Add(name);
            this.NewBranchBox.Clear();
            this.NewBranchBox.Focus();
        }

        private void AddButton_Click(Object sender, RoutedEventArgs e)
        {
            this.AddBranch();
        }

        private void NewBranchBox_KeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.AddBranch();
                e.Handled = true;
            }
        }

        private void RemoveButton_Click(Object sender, RoutedEventArgs e)
        {
            var branch = (sender as Button)?.Tag as String;

            if (branch != null)
                this.branches.Remove(branch);
        }

        private void SaveButton_Click(Object sender, RoutedEventArgs e)
        {
            Settings.Default.Branches = String.Join(",", this.branches);
            Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
