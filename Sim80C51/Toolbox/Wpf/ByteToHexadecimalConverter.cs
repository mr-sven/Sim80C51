using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sim80C51.Toolbox.Wpf
{
    [ValueConversion(typeof(byte), typeof(string))]
    public class ByteToHexadecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || targetType != typeof(string)) return DependencyProperty.UnsetValue;
            if (!byte.TryParse(value.ToString(), out byte byteValue)) return DependencyProperty.UnsetValue;
            if (parameter == null)
            {
                return byteValue.ToString("X2");
            }
            return byteValue.ToString(parameter.ToString());
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || targetType != typeof(byte)) return DependencyProperty.UnsetValue;
            string stringValue = value.ToString()!;
            byte returnValue;
            try { returnValue = System.Convert.ToByte(stringValue, 16); }
            catch { return DependencyProperty.UnsetValue; }
            return returnValue;
        }
    }
}
