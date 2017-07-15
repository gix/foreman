namespace Foreman
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;

    [Serializable]
    public class NodeLink : ISerializable
    {
        public ProductionNode Supplier { get; }
        public ProductionNode Consumer { get; }
        public Item Item { get; }
        public double Throughput { get; set; }

        private NodeLink(ProductionNode supplier, ProductionNode consumer, Item item,
            float maxAmount = float.PositiveInfinity)
        {
            Supplier = supplier;
            Consumer = consumer;
            Item = item;
        }

        public static bool CanLink(ProductionNode supplier, ProductionNode consumer, Item item)
        {
            return supplier.Supplies(item) && consumer.Consumes(item);
        }

        public static NodeLink Create(ProductionNode supplier, ProductionNode consumer, Item item,
            float maxAmount = float.PositiveInfinity)
        {
            if (!supplier.Supplies(item) || !consumer.Consumes(item))
                throw new InvalidOperationException($"Cannot connect {supplier} to {consumer} using item {item}");

            if (consumer.InputLinks.Any(l => l.Item == item && l.Supplier == supplier))
                return null;
            if (supplier.OutputLinks.Any(l => l.Item == item && l.Consumer == consumer))
                return null;
            NodeLink link = new NodeLink(supplier, consumer, item, maxAmount);
            supplier.OutputLinks.Add(link);
            consumer.InputLinks.Add(link);
            supplier.Graph.InvalidateCaches();
            return link;
        }

        public void Destroy()
        {
            Supplier.OutputLinks.Remove(this);
            Consumer.InputLinks.Remove(this);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Supplier", Supplier.Graph.Nodes.IndexOf(Supplier));
            info.AddValue("Consumer", Consumer.Graph.Nodes.IndexOf(Consumer));
            info.AddValue("Item", Item.Name);
        }
    }
}
