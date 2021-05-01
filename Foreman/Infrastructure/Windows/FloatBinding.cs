namespace Foreman.Infrastructure.Windows
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using Extensions;

    public class FloatBinding : Bind, IValueConverter
    {
        private string effectiveStringFormat;
        private string actualStringValue;

        public FloatBinding()
        {
            Converter = this;
        }

        public FloatBinding(string path)
            : base(path)
        {
            Converter = this;
        }

        public FloatBinding(PropertyPath path)
        {
            Path = new PropertyPath(path);
            Converter = this;
        }

        public NumberStyles NumberStyles { get; set; } = NumberStyles.Float;

        object IValueConverter.Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (actualStringValue != null) {
                value = actualStringValue;
                actualStringValue = null;
                return value;
            }

            if (value == null)
                return DoNothing;

            try {
                double doubleValue = Convert.ToDouble(value);
                if (!string.IsNullOrEmpty(StringFormat))
                    return string.Format(culture, EffectiveStringFormat, value);
                return doubleValue.ToString(StringFormat, culture);
            } catch (Exception ex) when (!ex.IsCriticalApplicationException()) {
                return DoNothing;
            } catch {
                // non CLS compliant exception
                return DoNothing;
            }
        }

        private string EffectiveStringFormat =>
            effectiveStringFormat ??= GetEffectiveStringFormat(StringFormat);

        object IValueConverter.ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && double.TryParse(s, NumberStyles, culture, out double result)) {
                if (((IValueConverter)this).Convert(result, typeof(string), parameter, culture) != value)
                    actualStringValue = s;
                return result;
            }

            return DoNothing;
        }

        private static string GetEffectiveStringFormat(string stringFormat)
        {
            if (stringFormat.IndexOf('{') < 0)
                stringFormat = "{0:" + stringFormat + "}";
            return stringFormat;
        }
    }
}