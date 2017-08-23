namespace Foreman.Extensions
{
    using System;
    using System.IO;
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

        public static BitmapSource LoadImage(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
                return LoadImage(stream);
        }

        public static BitmapSource LoadImage(Stream source)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = source;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
