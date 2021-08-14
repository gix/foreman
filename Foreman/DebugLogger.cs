namespace Foreman
{
    using System.Diagnostics;

    public class DebugLogger : ILogger
    {
        public void Log(string format, params object?[] args)
        {
            Debugger.Log(0, null, string.Format(format, args));
        }
    }
}
