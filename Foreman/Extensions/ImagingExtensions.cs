namespace Foreman.Extensions
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public static class ImagingExtensions
    {
        public static BitmapSource Resized(this BitmapSource image, int width, int height)
        {
            return new TransformedBitmap(
                image,
                new ScaleTransform(
                    width / (double)image.PixelWidth,
                    height / (double)image.PixelHeight));
        }

        public static Color ComputeAvgColor(this BitmapSource image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (image.Format != PixelFormats.Bgra32 &&
                image.Format != PixelFormats.Pbgra32)
                throw new NotImplementedException($"Unsupported pixel format {image.Format}");

            var bytes = new byte[4];
            image.Resized(1, 1).CopyPixels(bytes, 4, 0);
            return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public static BitmapSource LoadImage(string filePath, int? iconSize = null)
        {
            using var stream = File.OpenRead(filePath);
            return LoadImage(stream, iconSize);
        }

        public static BitmapSource LoadImage(Stream source, int? iconSize = null)
        {
            if (!source.CanSeek) {
                // BitmapImage assumes that unseekable streams are downloaded
                // from the web and starts a background thread. Cache the
                // deflate stream manually.
                source = CacheStream(source);
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = source;
            if (iconSize != null) {
                int s = iconSize.Value;
                image.SourceRect = new Int32Rect(0, 0, s, s);
            }
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static MemoryStream CacheStream(this Stream stream)
        {
            var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer;
        }
    }
}
