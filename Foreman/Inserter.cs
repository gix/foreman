namespace Foreman
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Media.Imaging;

    public class Inserter
    {
        private string? friendlyName;

        public Inserter(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public float RotationSpeed { get; set; }
        public BitmapSource? Icon { get; set; }

        [AllowNull]
        public string FriendlyName
        {
            get => !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : Name;
            set => friendlyName = value;
        }
    }
}
