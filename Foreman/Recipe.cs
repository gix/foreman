namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Imaging;

    public class Recipe
    {
        public string Name { get; }
        public float Time { get; }
        public string Category { get; set; }
        public Dictionary<Item, float> Results { get; }
        public Dictionary<Item, float> Ingredients { get; }
        public bool IsMissingRecipe { get; set; }
        public bool IsCyclic { get; set; }
        private BitmapSource uniqueIcon;

        public BitmapSource Icon
        {
            get
            {
                if (uniqueIcon != null) {
                    return uniqueIcon;
                }
                if (Results.Count == 1) {
                    return Results.Keys.First().Icon;
                }
                return DataCache.Current.UnknownIcon;
            }
            set => uniqueIcon = value;
        }

        public string FriendlyName
        {
            get
            {
                if (DataCache.Current.TryGetLocalizedString("recipe-name", Name, out var friendlyName))
                    return friendlyName;
                if (Results.Count == 1)
                    return Results.Keys.First().FriendlyName;
                return Name;
            }
        }

        public bool Enabled { get; set; }

        public Recipe(string name, float time, Dictionary<Item, float> ingredients, Dictionary<Item, float> results)
        {
            Name = name;
            Time = time;
            Ingredients = ingredients;
            Results = results;
            Enabled = true; //Nothing will have been loaded yet to disable recipes.
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Recipe recipe) {
                return false;
            }

            return recipe == this;
        }

        public static bool operator ==(Recipe recipe1, Recipe recipe2)
        {
            if (ReferenceEquals(recipe1, recipe2)) {
                return true;
            }

            if (recipe1 is null || recipe2 is null) {
                return false;
            }

            return recipe1.Name == recipe2.Name;
        }

        public static bool operator !=(Recipe recipe1, Recipe recipe2)
        {
            return !(recipe1 == recipe2);
        }
    }
}
