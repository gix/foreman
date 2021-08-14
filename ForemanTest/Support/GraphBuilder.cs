namespace ForemanTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Foreman;

    // A fluid interface for building up production graphs for testing. See references for usage.
    public class GraphBuilder
    {
        private static int counter;
        protected static int GetSequence()
        {
            counter += 1;
            return counter;
        }

        private readonly List<Tuple<ProductionNodeBuilder, ProductionNodeBuilder>> links;
        private readonly ISet<ProductionNodeBuilder> nodes;

        protected GraphBuilder()
        {
            links = new List<Tuple<ProductionNodeBuilder, ProductionNodeBuilder>>();
            nodes = new HashSet<ProductionNodeBuilder>();
        }

        public static GraphBuilder Create()
        {
            return new GraphBuilder();
        }

        internal SingletonNodeBuilder Supply(string item)
        {
            var node = new SingletonNodeBuilder(SupplyNode.Create).Item(item);
            nodes.Add(node);
            return node;
        }


        public SingletonNodeBuilder Consumer(string item)
        {
            var node = new SingletonNodeBuilder(ConsumerNode.Create).Item(item);
            nodes.Add(node);
            return node;
        }

        internal RecipeBuilder Recipe(string? name = null)
        {
            var node = new RecipeBuilder(name);
            nodes.Add(node);
            return node;
        }

        internal SingletonNodeBuilder Passthrough(string item)
        {
            var node = new SingletonNodeBuilder(PassthroughNode.Create).Item(item);
            nodes.Add(node);
            return node;
        }

        // Link the provided nodes by automatically matching up inputs to outputs.
        // The same builder can be passed to multiple different invocations, to enable building of complex graphs.
        internal void Link(params ProductionNodeBuilder[] nodeBuilders)
        {
            var bs = (IEnumerable<ProductionNodeBuilder>)nodeBuilders;
            var pairs = bs.Zip(bs.Skip(1), Tuple.Create);

            links.AddRange(pairs);
        }

        internal BuiltData Build()
        {
            var graph = new ProductionGraph();

            foreach (var node in nodes) {
                node.Build(graph);
            }

            foreach (var link in links) {
                var lhs = link.Item1;
                var rhs = link.Item2;

                foreach (var item in lhs.Built.Outputs.Intersect(rhs.Built.Inputs)) {
                    NodeLink.Create(lhs.Built, rhs.Built, item);
                }
            }
            return new BuiltData(graph);
        }

        public abstract class ProductionNodeBuilder
        {

            public ProductionNode Built { get; protected set; } = null!; // TODO: Build if not already
            internal abstract void Build(ProductionGraph graph);
        }

        public class SingletonNodeBuilder : ProductionNodeBuilder
        {
            private readonly Func<Item, ProductionGraph, ProductionNode> createFunction;

            public SingletonNodeBuilder(Func<Item, ProductionGraph, ProductionNode> f)
            {
                createFunction = f;
            }

            public string itemName { get; private set; } = "";
            public float target { get; private set; }

            internal SingletonNodeBuilder Item(string item)
            {
                itemName = item;
                return this;
            }

            internal SingletonNodeBuilder Target(float target)
            {
                this.target = target;
                return this;
            }

            internal override void Build(ProductionGraph graph)
            {
                Built = createFunction(new Item(itemName), graph);

                if (target > 0) {
                    Built.DesiredRate = target;
                    Built.RateType = RateType.Manual;
                } else {
                    Built.RateType = RateType.Auto;
                }
            }
        }

        internal class RecipeBuilder : ProductionNodeBuilder
        {
            private Dictionary<string, float> inputs;
            private Dictionary<string, float> outputs;
            private string? name;
            private double efficiency;

            public float target { get; private set; }

            internal RecipeBuilder(string? name)
            {
                inputs = new Dictionary<string, float>();
                outputs = new Dictionary<string, float>();
                this.name = name;
            }

            internal override void Build(ProductionGraph graph)
            {
                var duration = 1;
                name ??= "recipe-" + GetSequence();

                var recipe = new Recipe(name, duration, itemizeKeys(inputs), itemizeKeys(outputs));
                Built = RecipeNode.Create(recipe, graph);
                Built.BeaconModules.OverrideProductivityBonus = efficiency;

                if (target > 0) {
                    Built.DesiredRate = target;
                    Built.RateType = RateType.Manual;
                } else {
                    Built.RateType = RateType.Auto;
                }
            }

            internal RecipeBuilder Input(string itemName, float amount)
            {
                inputs.Add(itemName, amount);
                return this;
            }

            internal RecipeBuilder Output(string itemName, float amount)
            {
                outputs.Add(itemName, amount);
                return this;
            }

            internal RecipeBuilder Target(float target)
            {
                this.target = target;
                return this;
            }

            internal RecipeBuilder Efficiency(double bonus)
            {
                efficiency = bonus;
                return this;
            }

            private Dictionary<Item, float> itemizeKeys(Dictionary<string, float> d)
            {
                return d.ToDictionary(kp => new Item(kp.Key), kp => kp.Value);
            }
        }

        public class BuiltData
        {
            public ProductionGraph Graph { get; internal set; }

            public BuiltData(ProductionGraph graph)
            {
                Graph = graph;
            }

            public float SupplyRate(string itemName)
            {
                return Suppliers(itemName).Where(x => x is SupplyNode).Select(x => x.ActualRate).Sum();
            }

            private IEnumerable<ProductionNode> Suppliers(string itemName)
            {
                return Graph.GetSuppliers(new Item(itemName));
            }

            public float ConsumedRate(string itemName)
            {
                return Consumers(itemName).Where(x => x is ConsumerNode).Select(x => x.ActualRate).Sum();
            }

            private IEnumerable<ProductionNode> Consumers(string itemName)
            {
                return Graph.GetConsumers(new Item(itemName));
            }

            public float RecipeRate(string name)
            {
                return Graph.Nodes
                   .Where(x => x is RecipeNode node && node.BaseRecipe.Name == name)
                   .Select(x => x.ActualRate)
                   .Sum();
            }

            internal double RecipeInputRate(string name, string itemName)
            {
                return Graph.Nodes
                   .Where(x => x is RecipeNode node && node.BaseRecipe.Name == name)
                   .Select(x => (RecipeNode)x)
                   .First()
                   .GetSuppliedRate(new Item(itemName));
            }
        }
    }
}
