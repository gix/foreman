namespace Foreman
{
    using System.Windows.Media.Imaging;

    public class Entity
    {
        public Entity(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public BitmapSource Icon { get; set; }
    }
}
