namespace Foreman
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class Recipe
    {
        public string Name { get; }
        public float Time { get; }
        public string Category { get; set; }
        public Dictionary<Item, float> Results { get; }
        public Dictionary<Item, float> Ingredients { get; }
        public bool IsMissingRecipe { get; set; } = false;
        public bool IsCyclic { get; set; }
        private Bitmap uniqueIcon;

        public Bitmap Icon
        {
            get
            {
                if (uniqueIcon != null) {
                    return uniqueIcon;
                }
                if (Results.Count == 1) {
                    return Results.Keys.First().Icon;
                }
                return DataCache.UnknownIcon;
            }
            set => uniqueIcon = value;
        }

        public string FriendlyName
        {
            get
            {
                if (DataCache.LocaleFiles.ContainsKey("recipe-name") &&
                    DataCache.LocaleFiles["recipe-name"].ContainsKey(Name)) {
                    return DataCache.LocaleFiles["recipe-name"][Name];
                }
                if (Results.Count == 1) {
                    return Results.Keys.First().FriendlyName;
                }
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
            if (!(obj is Recipe)) {
                return false;
            }

            return (obj as Recipe) == this;
        }

        public static bool operator ==(Recipe recipe1, Recipe recipe2)
        {
            if (ReferenceEquals(recipe1, recipe2)) {
                return true;
            }

            if ((object)recipe1 == null || (object)recipe2 == null) {
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
