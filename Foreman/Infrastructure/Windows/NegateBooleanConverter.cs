namespace Foreman.Infrastructure.Windows
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class NegateBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool v)
                return !v;
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool v)
                return !v;
            return Binding.DoNothing;
        }
    }
}
