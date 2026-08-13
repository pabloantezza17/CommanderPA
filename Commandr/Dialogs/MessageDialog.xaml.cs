using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Commandr.Dialogs
{
    /// <summary>
    /// Tipo de diálogo: define el color de acento y el glifo del badge.
    /// </summary>
    public enum DialogKind
    {
        Question,
        Stopped,
        Success,
        Failed
    }

    /// <summary>
    /// Diálogo modal con la estética de las ventanas de la app, en reemplazo del MessageBox
    /// del sistema. Se usa para confirmaciones y para avisos de fin de proceso.
    /// </summary>
    public partial class MessageDialog : Window
    {
        #region Members

        private static readonly Color AmberColor = Color.FromRgb(0xE5, 0x9A, 0x1F);
        private static readonly Color GreenColor = Color.FromRgb(0x22, 0xA6, 0x5B);
        private static readonly Color RedColor = Color.FromRgb(0xE5, 0x48, 0x4D);

        #endregion

        #region Constructor

        private MessageDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>Pide confirmación. Devuelve true si el usuario aceptó.</summary>
        public static Boolean Confirm(String title, String message, String acceptText, String cancelText, DialogKind kind = DialogKind.Question)
        {
            return OnUi(() =>
            {
                MessageDialog dialog = Create(title, message, null, kind);

                dialog.AcceptButton.Content = acceptText;
                dialog.CancelButton.Content = cancelText;

                return dialog.ShowDialog() == true;
            });
        }

        /// <summary>Muestra un aviso con un solo botón de cierre.</summary>
        public static void Info(String title, String message, String detail, DialogKind kind)
        {
            OnUi(() =>
            {
                MessageDialog dialog = Create(title, message, detail, kind);

                dialog.AcceptButton.Content = "Cerrar";
                dialog.CancelButton.Visibility = Visibility.Collapsed;

                dialog.ShowDialog();

                return true;
            });
        }

        private static MessageDialog Create(String title, String message, String detail, DialogKind kind)
        {
            MessageDialog dialog = new MessageDialog();

            dialog.TitleText.Text = title;
            dialog.MessageText.Text = message;

            if (String.IsNullOrEmpty(message))
                dialog.MessageText.Visibility = Visibility.Collapsed;

            if (String.IsNullOrEmpty(detail))
                dialog.DetailText.Visibility = Visibility.Collapsed;
            else
                dialog.DetailText.Text = detail;

            dialog.ApplyKind(kind);
            dialog.AttachToOwner();

            return dialog;
        }

        private void ApplyKind(DialogKind kind)
        {
            Color accent;
            String glyph;

            switch (kind)
            {
                case DialogKind.Stopped:
                    accent = AmberColor;
                    glyph = "■";
                    break;
                case DialogKind.Success:
                    accent = GreenColor;
                    glyph = "✓";
                    break;
                case DialogKind.Failed:
                    accent = RedColor;
                    glyph = "✕";
                    break;
                default:
                    accent = AmberColor;
                    glyph = "?";
                    break;
            }

            this.BadgeGlyph.Text = glyph;
            this.BadgeGlyph.Foreground = new SolidColorBrush(accent);
            this.BadgeBack.Fill = new SolidColorBrush(Tint(accent));
            this.AcceptButton.Background = new SolidColorBrush(accent);
        }

        /// <summary>Versión clara del acento, para el fondo del badge.</summary>
        private static Color Tint(Color color)
        {
            return Color.FromRgb(
                (Byte)(color.R + (255 - color.R) * 0.88),
                (Byte)(color.G + (255 - color.G) * 0.88),
                (Byte)(color.B + (255 - color.B) * 0.88));
        }

        /// <summary>
        /// Centra el diálogo sobre la ventana activa. Si no hay ninguna visible (el proceso pudo
        /// arrancar desde el tray), lo centra en la pantalla.
        /// </summary>
        private void AttachToOwner()
        {
            Window owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w != this && w.IsVisible && w.IsActive);

            if (owner == null)
                owner = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w != this && w.IsVisible);

            if (owner != null)
                this.Owner = owner;
            else
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        /// <summary>Corre la acción en el hilo de UI: los avisos de fin llegan de hilos de fondo.</summary>
        private static Boolean OnUi(Func<Boolean> action)
        {
            if (Application.Current == null)
                return false;

            if (Application.Current.Dispatcher.CheckAccess())
                return action();

            return Application.Current.Dispatcher.Invoke(action);
        }

        #endregion

        #region Commands

        private void Accept_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /// <summary>Escape cierra el diálogo como si se hubiera cancelado.</summary>
        private void Window_PreviewKeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            this.DialogResult = false;
            e.Handled = true;
        }

        /// <summary>La ventana no tiene barra de título: se arrastra desde cualquier punto.</summary>
        private void Window_MouseLeftButtonDown(Object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch (Exception)
            {
                // DragMove tira si el botón ya se soltó; es cosmético.
            }
        }

        #endregion
    }
}
