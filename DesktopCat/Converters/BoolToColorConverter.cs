using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopCat.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public System.Windows.Media.Brush TrueColor { get; set; } = System.Windows.Media.Brushes.Green;
        public System.Windows.Media.Brush FalseColor { get; set; } = System.Windows.Media.Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return TrueColor;
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
