namespace Foreman.Extensions
{
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    public static class IOExtensions
    {
        public static string ReadAllText(this Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            return reader.ReadToEnd();
        }

        public static string ReadAllText(this ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            return stream.ReadAllText();
        }
    }
}
