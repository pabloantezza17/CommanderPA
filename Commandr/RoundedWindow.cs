using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Commandr
{
    /// <summary>
    /// Pide al compositor de Windows 11 (DWM) que dibuje la ventana con esquinas redondeadas y un
    /// borde fino del color indicado. En Windows 10 las llamadas no hacen nada y la ventana queda
    /// como siempre, así que es seguro usarlo en cualquier versión.
    /// </summary>
    public static class RoundedWindow
    {
        private const Int32 DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const Int32 DWMWA_BORDER_COLOR = 34;
        private const Int32 DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern Int32 DwmSetWindowAttribute(IntPtr hwnd, Int32 attribute, ref Int32 value, Int32 size);

        /// <summary>Llamar desde <c>OnSourceInitialized</c>, cuando la ventana ya tiene handle.</summary>
        public static void Apply(Window window, Color borderColor)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero) return;

            Int32 corner = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(Int32));

            // COLORREF es 0x00BBGGRR.
            Int32 colorRef = borderColor.R | (borderColor.G << 8) | (borderColor.B << 16);
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref colorRef, sizeof(Int32));
        }
    }
}
