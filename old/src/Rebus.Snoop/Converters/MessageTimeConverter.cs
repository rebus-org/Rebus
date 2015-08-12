using System;
using System.Globalization;
using System.Windows.Data;

namespace Rebus.Snoop.Converters
{
    public class MessageTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime)) return value;

            var time = (DateTime) value;

            return time.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}