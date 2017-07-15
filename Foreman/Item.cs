namespace Foreman
{
    using System.Collections.Generic;
    using System.Windows.Media.Imaging;

    public class Item
    {
        public static readonly List<string> LocaleCategories =
            new List<string> {"item-name", "fluid-name", "entity-name", "equipment-name"};

        public string Name { get; }
        public HashSet<Recipe> Recipes { get; }
        public BitmapSource Icon { get; set; }

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
