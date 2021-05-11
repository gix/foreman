namespace Foreman
{
    using System.Windows;
    using Controls;

    public abstract class GraphElement : ViewModel, IInteractiveElement
    {
        private bool isSelected;

        protected GraphElement()
        {
        }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public abstract bool IsDraggable { get; }
        public abstract bool IsSelectable { get; }

        public HorizontalAlignment HorizontalAlignment { get; protected set; } = HorizontalAlignment.Stretch;
        public VerticalAlignment VerticalAlignment { get; protected set; } = VerticalAlignment.Stretch;

        public GraphElement Clone()
        {
            GraphElement cloned = CreateInstanceCore();
            cloned.CloneCore(this);
            return cloned;
        }

        protected virtual GraphElement CreateInstanceCore()
        {
            return null;
        }

        protected virtual void CloneCore(GraphElement source)
        {
            IsSelected = source.IsSelected;
            HorizontalAlignment = source.HorizontalAlignment;
            VerticalAlignment = source.VerticalAlignment;
        }
    }
}