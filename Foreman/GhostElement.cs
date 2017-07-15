namespace Foreman
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;

    public class GhostElement : ViewModel
    {
        public GhostElement(IEnumerable<ImageSource> icons)
        {
            Icons = icons.ToList();
        }

        public IReadOnlyList<ImageSource> Icons { get; }
    }
}
