using System;
using System.Globalization;
using System.Windows.Data;

namespace WloCharacterViewer
{
    public class PercentToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = System.Convert.ToDouble(value);
            double maxWidth = parameter != null ? double.Parse(parameter.ToString()!) : 400;
            return Math.Max(0, Math.Min(maxWidth, percent / 100.0 * maxWidth));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
