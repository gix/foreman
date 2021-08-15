namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using Foreman.Extensions;

    public enum PinKind
    {
        Input = 0,
        Output = 1
    }

    public class Pin : GraphElement
    {
        private readonly HashSet<Connector> connectors = new();
        private string label;
        private ImageSource? icon;
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
            label = string.Empty;
            balloonText = $"{Item.FriendlyName}\nDrag to create a new connection";
        }

        public event EventHandler<EventArgs>? HotspotUpdated;
        public event EventHandler<EventArgs>? ConnectionChanged;

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

        public ImageSource? Icon
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

        public bool IsConnected => connectors.Count != 0;

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

        public NodeElement? GetConnectedNode()
        {
            if (Connectors.Count == 0)
                return null;

            if (Kind == PinKind.Input)
                return Connectors.First().Source?.Node;
            else
                return Connectors.First().Destination?.Node;
        }

        public IEnumerable<NodeElement> GetConnectedNodes()
        {
            if (Kind == PinKind.Input)
                return Connectors.Select(x => x.Source?.Node).NotNull();
            else
                return Connectors.Select(x => x.Destination?.Node).NotNull();
        }

        protected override GraphElement CreateInstanceCore()
        {
            return new Pin(Kind, Item, Node);
        }

        internal void RaiseConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
