using System.Globalization;
using System.Windows.Data;

namespace Sim80C51.Toolbox.Wpf
{
    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return ((double)value) > double.Parse(parameter as string ?? string.Empty);
            }
            return ((int)value) > int.Parse(parameter as string ?? string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
