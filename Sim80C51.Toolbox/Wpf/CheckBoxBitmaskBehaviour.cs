using System.Windows;
using System.Windows.Controls;

namespace Sim80C51.Toolbox.Wpf
{
    public class CheckBoxBitmaskBehaviour : DependencyObject
    {
        public static readonly DependencyProperty MaskProperty = DependencyProperty.RegisterAttached("Mask", typeof(byte), typeof(CheckBoxBitmaskBehaviour), new UIPropertyMetadata((byte)0));

        public static byte GetMask(DependencyObject obj)
        {
            return (byte)obj.GetValue(MaskProperty);
        }

        public static void SetMask(DependencyObject obj, byte value)
        {
            obj.SetValue(MaskProperty, value);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(byte), typeof(CheckBoxBitmaskBehaviour), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged));

        public static byte GetValue(DependencyObject obj)
        {
            return (byte)obj.GetValue(ValueProperty);
        }

        public static void SetValue(DependencyObject obj, byte value)
        {
            obj.SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(CheckBoxBitmaskBehaviour), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsCheckedChanged));

        public static bool? GetIsChecked(DependencyObject obj)
        {
            return (bool?)obj.GetValue(IsCheckedProperty);
        }

        public static void SetIsChecked(DependencyObject obj, bool? value)
        {
            obj.SetValue(IsCheckedProperty, value);
        }

        private static bool isValueChanging;
        private static bool isIsCheckedChanging;

        private static void ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (isIsCheckedChanging) { return; }
            isValueChanging = true;
            byte mask = GetMask(obj);
            byte value = (byte)e.NewValue;
            SetIsChecked(obj, (value & mask) != 0);
            isValueChanging = false;
        }

        private static void IsCheckedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if(isValueChanging) { return; }
            if (e.NewValue is bool isChecked)
            {
                isIsCheckedChanging = true;
                byte mask = GetMask(obj);
                byte value = GetValue(obj);
                if (isChecked)
                {
                    value |= mask;
                }
                else
                {
                    value &= (byte)(~mask);
                }
                SetValue(obj, value);
                isIsCheckedChanging = false;
            }
        }
    }
}
