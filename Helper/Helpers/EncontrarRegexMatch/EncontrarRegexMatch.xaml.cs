using Framework;
using Framework.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Helper
{
    public partial class EncontrarRegexMatch : ObservableWindow
    {
        #region Constructor

        public EncontrarRegexMatch()
        {
            InitializeComponent();

            this.chkMultiline.IsChecked = true;
            this.chkIgnoreCase.IsChecked = true;
        }

        #endregion

        #region Attributes and Properties

        private String[] archivo;

        private String[] Archivo
        {
            get
            {
                if (this.archivo == null) return Enumerable.Empty<String>().ToArray();

                return this.archivo;
            }
            set { this.archivo = value; }
        }

        public IEnumerable<Match> Matches { get; private set; }

        #endregion

        #region Methods

        private void SelectPath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Todos los Archivos (*.*)|*.*" };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    this.txtPath.Text = openFileDialog.FileName;
                    this.LoadFile();
                    this.ReloadFile();
                }
                catch (Exception ex)
                {
                    Utils.HandleException(ex);
                }
            }
        }

        private void LoadFile()
        {
            try
            {
                this.Archivo = File.ReadAllLines(txtPath.Text, Encoding.Default);
            }
            catch (Exception ex)
            {
                this.Archivo = null;

                Utils.HandleException(ex);
            }
        }

        private void ReloadFile()
        {
            this.txtText.Text = String.Join(Environment.NewLine, this.Archivo);
        }

        private void LookForMatches()
        {
            try
            {
                var resultados = new List<String>();

                var regex = this.BuildRegex();

                var matches = regex.Matches(this.txtText.Text);

                this.treeCaptures.Items.Clear();

                this.Matches = matches.Cast<Match>();

                foreach (Match match in matches)
                {
                    var treeViewItem = new TreeViewItem { Header = match.Value };

                    foreach (var capture in match.Groups.Cast<Group>().Skip(1))
                        treeViewItem.Items.Add(capture);

                    this.treeCaptures.Items.Add(treeViewItem);

                    resultados.Add(match.Value);
                }

                this.txtResult.Text = String.Join(Environment.NewLine, resultados);
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }
        }

        private Regex BuildRegex()
        {
            var pattern = this.txtPattern.Text;

            var regexOptions = RegexOptions.None;

            if (this.chkCultureInvariant.IsChecked == true)
                regexOptions |= RegexOptions.CultureInvariant;

            if (this.chkIgnoreCase.IsChecked == true)
                regexOptions |= RegexOptions.IgnoreCase;

            if (this.chkMultiline.IsChecked == true)
                regexOptions |= RegexOptions.Multiline;

            if (this.chkSingleline.IsChecked == true)
                regexOptions |= RegexOptions.Singleline;

            if (this.chkIgnorePatternWhitespace.IsChecked == true)
                regexOptions |= RegexOptions.IgnorePatternWhitespace;

            if (this.chkRightToLeft.IsChecked == true)
                regexOptions |= RegexOptions.RightToLeft;

            return new Regex(pattern, regexOptions);
        }

        #endregion

        #region Events

        private void BtnPath_Click(Object sender, RoutedEventArgs e)
        {
            this.SelectPath();
        }

        private void TxtPath_KeyUp(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    this.LoadFile();
                    this.ReloadFile();
                }
                catch (Exception ex)
                {
                    Utils.HandleException(ex);
                }
            }
        }

        private void BtnReload_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                this.ReloadFile();
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }
        }

        private void TxtPattern_KeyUp(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.LookForMatches();
            }
        }

        private void BtnMatch_Click(Object sender, RoutedEventArgs e)
        {
            this.LookForMatches();
        }

        private void BtnCopy_Click(Object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.txtResult.Text);
        }

        private void ShowCapturesTreeButton_Click(Object sender, RoutedEventArgs e)
        {
            this.treeCaptures.Visibility = this.treeCaptures.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void CopyCapturesToTemplaterButton_Click(Object sender, RoutedEventArgs e)
        {
            if (this.Matches == null || !this.Matches.Any())
            {
                MessageBox.Show("No matches found.");

                return;
            }

            new EditarDatosParaTemplater(this.Matches).ShowDialog();
        }

        #endregion
    }
}