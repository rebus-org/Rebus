using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rebus.Snoop.Converters
{
    public class NullVisibilityConverter : IValueConverter
    {
        public bool HideIfNull { get; set; }

        public bool ShowIfNull
        {
            get { return !HideIfNull; }
            set { HideIfNull = !value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = ReferenceEquals(null, value);

            return HideIfNull && isNull || ShowIfNull && !isNull
                       ? Visibility.Collapsed
                       : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}