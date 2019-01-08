using System;
using System.Globalization;
using System.Windows.Data;

namespace TinyVideoPlayer.Converters
{
    /// <summary>
    /// Allow the child to be centered inside the canvas.
    /// </summary>
    public class OffsetConverter : IValueConverter
    {
        /// <summary>
        /// Convert method.
        /// </summary>
        public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
        {
            var canvasWidth = System.Convert.ToDouble(values);
            switch ((string)parameter)
            {
                case "inside":
                    return canvasWidth - canvasWidth / 20 - 10;
                case "outside":
                    return canvasWidth - canvasWidth / 5;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
