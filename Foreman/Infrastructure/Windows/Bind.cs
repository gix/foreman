namespace Foreman.Infrastructure.Windows
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class Bind : Binding
    {
        public Bind()
        {
            ConverterCulture = CultureInfo.CurrentCulture;
        }

        public Bind(string path)
            : base(path)
        {
            ConverterCulture = CultureInfo.CurrentCulture;
        }

        public Bind(PropertyPath path)
        {
            Path = new PropertyPath(path);
            ConverterCulture = CultureInfo.CurrentCulture;
        }
    }
}
