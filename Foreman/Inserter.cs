namespace Foreman
{
    using System.Drawing;

    public class Inserter
    {
        public string Name { get; }
        public float RotationSpeed { get; set; }
        public Bitmap Icon { get; set; }
        private string friendlyName;

        public string FriendlyName
        {
            get => !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : Name;
            set => friendlyName = value;
        }

        public Inserter(string name)
        {
            Name = name;
        }
    }
}
