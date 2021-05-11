namespace Foreman.Extensions
{
    using System.Windows.Threading;

    public static class DispatcherExtensions
    {
        public static void Flush(
            this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.SystemIdle)
        {
            dispatcher.Invoke(() => { }, priority);
        }
    }
}
