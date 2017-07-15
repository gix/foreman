namespace Foreman.Extensions
{
    using System.Windows;

    public static class DataObjectExtensions
    {
        public static bool IsDataPresent<T>(this IDataObject obj)
        {
            return obj.GetDataPresent(typeof(T));
        }

        public static T GetData<T>(this IDataObject obj)
        {
            return (T)obj.GetData(typeof(T));
        }
    }
}
