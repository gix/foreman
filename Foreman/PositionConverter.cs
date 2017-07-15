namespace Foreman
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;

    public sealed class PositionConverter : TypeConverter
    {
        private readonly LengthConverter lengthConverter = new LengthConverter();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return
                sourceType == typeof(string) ||
                base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return
                destinationType == typeof(string) ||
                base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                throw GetConvertFromException(value);

            if (value is string source)
                return Parse(context, source);

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(
            ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is Point point)
                return ConvertToString(context, point);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        private Point Parse(ITypeDescriptorContext context, string source)
        {
            var invariantCulture = CultureInfo.InvariantCulture;
            var tokenizerHelper = new TokenizerHelper(source, invariantCulture);
            var x = (double)lengthConverter.ConvertFrom(
                context, invariantCulture, tokenizerHelper.NextTokenRequired());
            var y = (double)lengthConverter.ConvertFrom(
                context, invariantCulture, tokenizerHelper.NextTokenRequired());
            tokenizerHelper.LastTokenRequired();
            return new Point(x, y);
        }

        private string ConvertToString(ITypeDescriptorContext context, Point point)
        {
            var invariantCulture = CultureInfo.InvariantCulture;
            char separator = TokenizerHelper.GetNumericListSeparator(invariantCulture);
            var xStr = (string)lengthConverter.ConvertTo(
                context, invariantCulture, point.X, typeof(string));
            var yStr = (string)lengthConverter.ConvertTo(
                context, invariantCulture, point.Y, typeof(string));
            return $"{xStr}{separator}{yStr}";
        }
    }
}
