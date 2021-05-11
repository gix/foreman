namespace Foreman
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class Connector : GraphElement
    {
        private Pin? source;
        private Pin? destination;

        private Point sourceHotspot;
        private Point destinationHotspot;

        private PointCollection? points;
        private Color fillColor;

        public Connector(NodeLink displayedLink, Pin? source, Pin? destination)
        {
            DisplayedLink = displayedLink;
            Source = source;
            Destination = destination;
            FillColor = DataCache.IconAverageColor(displayedLink.Item.Icon);
        }

        public override bool IsDraggable => false;
        public override bool IsSelectable => true;

        public NodeLink DisplayedLink { get; }

        public Pin? Source
        {
            get => source;
            set
            {
                if (source == value)
                    return;

                if (source != null) {
                    source.RemoveConnector(this);
                    source.HotspotUpdated -= OnSourceHotspotUpdated;
                }

                source = value;

                if (source != null) {
                    source.AddConnector(this);
                    source.HotspotUpdated += OnSourceHotspotUpdated;
                    SourceHotspot = source.Hotspot;
                }

                RaisePropertyChanged();
                OnConnectionChanged();
            }
        }

        public Pin? Destination
        {
            get => destination;
            set
            {
                if (destination == value)
                    return;

                if (destination != null) {
                    destination.RemoveConnector(this);
                    destination.HotspotUpdated -= OnDestinationHotspotUpdated;
                }

                destination = value;

                if (destination != null) {
                    destination.AddConnector(this);
                    destination.HotspotUpdated += OnDestinationHotspotUpdated;
                    DestinationHotspot = destination.Hotspot;
                }

                RaisePropertyChanged();
                OnConnectionChanged();
            }
        }

        public Point SourceHotspot
        {
            get => sourceHotspot;
            set
            {
                if (SetProperty(ref sourceHotspot, value))
                    ComputeConnectorPoints();
            }
        }

        public Point DestinationHotspot
        {
            get => destinationHotspot;
            set
            {
                if (SetProperty(ref destinationHotspot, value))
                    ComputeConnectorPoints();
            }
        }

        public PointCollection? Points
        {
            get => points;
            set => SetProperty(ref points, value);
        }

        public Color FillColor
        {
            get => fillColor;
            set => SetProperty(ref fillColor, value);
        }

        public event EventHandler<EventArgs>? ConnectionChanged;

        private void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            Source?.RaiseConnectionChanged();
            Destination?.RaiseConnectionChanged();
        }

        private void OnSourceHotspotUpdated(object? sender, EventArgs e)
        {
            SourceHotspot = Source!.Hotspot;
        }

        private void OnDestinationHotspotUpdated(object? sender, EventArgs e)
        {
            DestinationHotspot = Destination!.Hotspot;
        }

        private void ComputeConnectorPoints()
        {
            if (Source != null && Destination!= null && Source.Node == Destination.Node) {
                var computedPoints = new PointCollection {
                    SourceHotspot,
                    SourceHotspot + new Vector(-Source.Node.RenderWidth, -250),
                    DestinationHotspot + new Vector(-Source.Node.RenderWidth, 250),
                    DestinationHotspot
                };
                computedPoints.Freeze();
                Points = computedPoints;
            } else {
                var computedPoints = new PointCollection { SourceHotspot, DestinationHotspot };
                computedPoints.Freeze();
                Points = computedPoints;
            }
        }
    }
}
