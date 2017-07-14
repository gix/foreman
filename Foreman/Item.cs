namespace Foreman
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;

    public class Item
    {
        public static readonly List<string> LocaleCategories =
            new List<string> {"item-name", "fluid-name", "entity-name", "equipment-name"};

        public string Name { get; }
        public HashSet<Recipe> Recipes { get; }
        public Bitmap Icon { get; set; }

        private Bitmap smallIcon;
        public Bitmap SmallIcon => smallIcon ?? (smallIcon = ResizeImage(Icon, 23, 23));

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public string FriendlyName
        {
            get
            {
                foreach (string category in LocaleCategories) {
                    if (DataCache.LocaleFiles.ContainsKey(category) &&
                        DataCache.LocaleFiles[category].ContainsKey(Name)) {
                        return DataCache.LocaleFiles[category][Name];
                    }
                }

                return Name;
            }
        }

        public bool IsMissingItem { get; set; } = false;

        private Item()
        {
            Name = "";
        }

        public Item(string name)
        {
            Name = name;
            Recipes = new HashSet<Recipe>();
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var item = obj as Item;
            return item != null && this == item;
        }

        public static bool operator ==(Item item1, Item item2)
        {
            if (ReferenceEquals(item1, item2)) {
                return true;
            }
            if ((object)item1 == null || (object)item2 == null) {
                return false;
            }

            return item1.Name == item2.Name;
        }

        public static bool operator !=(Item item1, Item item2)
        {
            return !(item1 == item2);
        }

        public override string ToString()
        {
            return string.Format("Item: {0}", Name);
        }
    }
}
