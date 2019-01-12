using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TinyVideoPlayer.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool) value && parameter == null)
                return Visibility.Visible;
            if ((bool) value == false && parameter == null)
                return Visibility.Collapsed;
            if ((bool) value && parameter.ToString() == "invert")
                return Visibility.Collapsed;
            if ((bool) value == false && parameter.ToString() == "invert")
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
