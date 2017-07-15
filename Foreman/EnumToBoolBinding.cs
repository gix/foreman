namespace Foreman
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Markup;

    public class EnumToBoolBinding : Binding, IValueConverter
    {
        public EnumToBoolBinding()
        {
            Converter = this;
        }

        public EnumToBoolBinding(string path)
            : base(path)
        {
            Converter = this;
        }

        public EnumToBoolBinding(string path, object enumValue)
            : base(path)
        {
            Converter = this;
            ConverterParameter = enumValue;
        }

        [ConstructorArgument("enumValue")]
        public object EnumValue
        {
            get => ConverterParameter;
            set => ConverterParameter = value;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, true) ? parameter : DoNothing;
        }
    }
}
