using System;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;

namespace Commandr
{
    public class ProgressForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            Brush foreground = Brushes.Azure;

            if (progress >= 95d)
            {
                foreground = Brushes.Red;
            }
            else if (progress >= 90d)
            {
                foreground = Brushes.Yellow;
            }

            return foreground;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}