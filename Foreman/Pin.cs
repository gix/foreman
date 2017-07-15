namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    public enum PinKind
    {
        Input = 0,
        Output = 1
    }

    public class Pin : GraphElement
    {
        private readonly HashSet<Connector> connectors = new HashSet<Connector>();
        private string label;
        private ImageSource icon;
        private Point hotspot;
        private Color fillColor;
        private string balloonText;
        private bool isHighlighted;

        public Pin(PinKind kind, Item item, NodeElement node)
        {
            Kind = kind;
            Item = item;
            Node = node;
            Icon = item.Icon;
            Label = string.Empty;
            BalloonText = $"{Item.FriendlyName}\nDrag to create a new connection";
        }

        public event EventHandler<EventArgs> HotspotUpdated;
        public event EventHandler<EventArgs> ConnectionChanged;

        public override bool IsDraggable => false;
        public override bool IsSelectable => false;

        public PinKind Kind { get; }
        public Item Item { get; }
        public NodeElement Node { get; }

        public string Label
        {
            get => label;
            set => SetProperty(ref label, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public void AddConnector(Connector connector)
        {
            connectors.Add(connector);
        }

        public void RemoveConnector(Connector connector)
        {
            connectors.Remove(connector);
        }

        public IReadOnlyCollection<Connector> Connectors => connectors;

        public Point Hotspot
        {
            get => hotspot;
            set
            {
                if (SetProperty(ref hotspot, value))
                    HotspotUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        public Color FillColor
        {
            get => fillColor;
            set => SetProperty(ref fillColor, value);
        }

        public string BalloonText
        {
            get => balloonText;
            private set => SetProperty(ref balloonText, value);
        }

        public bool IsHighlighted
        {
            get => isHighlighted;
            set => SetProperty(ref isHighlighted, value);
        }

        internal void RaiseConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}