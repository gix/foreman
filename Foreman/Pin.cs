namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        private int? xOrder;

        public void ClearXOrder()
        {
            if (xOrder != null)
                Debug.WriteLine("[{0}] {1}: clear", Kind, Item);
            xOrder = null;
        }

        public int GetPinXOrder()
        {
            xOrder ??= ComputeXOrder();
            return xOrder.Value;
        }

        private int ComputeXOrder()
        {
            // This simple heuristic orders the pins on a node relative to the
            // position of their connected node. There is no attempt to prevent
            // overlap of connector lines.
            //
            // A pin connected to a node further to the left should come before
            // a pin connected to a node further to the right:
            //
            // |   +---+   |                |   +---+   |
            // +===|   |===+                +===|   |===+
            //     +-+-+                        +-+-+
            //       |                            |
            //       +-----+               +------+
            //             |               |
            //           +-+-+   +---+   +-+-+
            //       +===|   |===|   |===|   |===+
            //       |   +---+   +---+   +---+   |
            //       |                           |
            //
            // A pin connected to a node further up should come before a pin
            // connected to a node further down:
            //
            //                              |   +---+   |
            //                              +===|   |===+
            //                                  +-+-+
            //                     +--------------+
            //                     |
            //                     |        |   +---+   |
            //                     |        +===|   |===+
            //                     |            +-+-+
            //                     |       +------+
            //           +---+   +-+-+   +-+-+
            //       +===|   |===|   |===|   |===+
            //       |   +---+   +---+   +---+   |
            //       |                           |
            //
            // So just looking at the X coordinate is not enough.

            if (!IsConnected)
                return 0; // Keep unconnected pins neutral.

            var node = Node;
            var nodes = GetConnectedNodes();

            Point center1 = node.Position + ((Vector)node.RenderSize / 2);
            Point center2 = nodes.Select(x => x.Position + ((Vector)x.RenderSize / 2)).ComputeCentroid();

            double factorY = Kind == PinKind.Input ? 1 : -1;

            Vector diff = center2 - center1;
            diff.Y = Math.Max(0, factorY * diff.Y);

            int sort = Convert.ToInt32(Math.Atan2(diff.X, diff.Y) * 1000);

            Debug.WriteLine("[{0}] {1}: {2}, {3}, {4} -> {5}", Kind, Item, center1, center2, diff, sort);
            return sort;
        }
    }
}
