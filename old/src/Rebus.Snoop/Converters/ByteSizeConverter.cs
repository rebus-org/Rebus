using System;
using System.Globalization;
using System.Windows.Data;

namespace Rebus.Snoop.Converters
{
    public class ByteSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long valueToConvert;
            
            if (value is int)
            {
                valueToConvert = (int) value;
            }
            else if (value is long)
            {
                valueToConvert = (long) value;
            }
            else
            {
                return value;
            }

            var megaBytes = valueToConvert/Math.Pow(2, 20);
            var kiloBytes = valueToConvert/Math.Pow(2, 10);

            if (megaBytes >= 1)
            {
                return string.Format("{0:0.0} MB", megaBytes);
            }
            if (kiloBytes >= 1)
            {
                return string.Format("{0:0.0} kB", kiloBytes);
            }
            return string.Format("{0} B", valueToConvert);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}