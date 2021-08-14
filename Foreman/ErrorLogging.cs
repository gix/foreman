namespace Foreman
{
    using System.IO;
    using System.Windows.Forms;

    public static class ErrorLogging
    {
        public static void LogLine(string message)
        {
            try {
                File.AppendAllText(Path.Combine(Application.StartupPath, "errorlog.txt"), message + "\n");
            } catch {
                // ignored
            }
        }
    }
}
