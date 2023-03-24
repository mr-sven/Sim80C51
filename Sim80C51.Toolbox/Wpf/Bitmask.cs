using System.Windows;

namespace Sim80C51.Toolbox.Wpf
{
    public class Bitmask : DependencyObject
    {
        public static readonly DependencyProperty MaskProperty = DependencyProperty.RegisterAttached("Mask", typeof(byte), typeof(Bitmask), new UIPropertyMetadata((byte)0));
        public static byte GetMask(DependencyObject obj) { return (byte)obj.GetValue(MaskProperty); }
        public static void SetMask(DependencyObject obj, byte value) { obj.SetValue(MaskProperty, value); }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(byte), typeof(Bitmask), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged));
        public static byte GetValue(DependencyObject obj) { return (byte)obj.GetValue(ValueProperty); }
        public static void SetValue(DependencyObject obj, byte value) { obj.SetValue(ValueProperty, value); }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(Bitmask), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsCheckedChanged));
        public static bool? GetIsChecked(DependencyObject obj) { return (bool?)obj.GetValue(IsCheckedProperty); }
        public static void SetIsChecked(DependencyObject obj, bool? value) { obj.SetValue(IsCheckedProperty, value); }

        public static readonly DependencyProperty InvertedProperty = DependencyProperty.RegisterAttached("Inverted", typeof(bool), typeof(Bitmask), new UIPropertyMetadata(false));
        public static bool GetInverted(DependencyObject obj) { return (bool)obj.GetValue(InvertedProperty); }
        public static void SetInverted(DependencyObject obj, bool value) { obj.SetValue(InvertedProperty, value); }

        private static bool isValueChanging;
        private static bool isIsCheckedChanging;

        private static void ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (isIsCheckedChanging) { return; }
            isValueChanging = true;
            byte mask = GetMask(obj);
            byte value = (byte)e.NewValue;
            bool inverted = GetInverted(obj);
            bool isChecked = (value & mask) != 0;
            SetIsChecked(obj, inverted ? !isChecked : isChecked);
            isValueChanging = false;
        }

        private static void IsCheckedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (isValueChanging) { return; }
            if (e.NewValue is bool isChecked)
            {
                isIsCheckedChanging = true;
                byte mask = GetMask(obj);
                byte value = GetValue(obj);
                bool inverted = GetInverted(obj);
                isChecked = inverted ? !isChecked : isChecked;

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
