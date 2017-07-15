namespace Foreman
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class RenderSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.Length == 2 && value[0] is double w && value[1] is double h)
                return new Size(w, h);
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            if (value is Size size)
                return new object[] { size.Width, size.Height };
            return null;
        }
    }
}
