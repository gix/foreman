namespace Foreman
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class ColorToBrushConverter : IValueConverter
    {
        public bool IgnoreAlpha { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Color color || !targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                return DependencyProperty.UnsetValue;

            if (IgnoreAlpha)
                color.A = 255;
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not SolidColorBrush brush || targetType != typeof(Color))
                return Binding.DoNothing;

            return brush.Color;
        }
    }
}
