namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;

    public enum NodeType
    {
        Recipe,
        Supply,
        Consumer
    }

    public enum RateType
    {
        Auto,
        Manual
    }

    [Serializable]
    public abstract partial class ProductionNode : ISerializable
    {
        public static readonly int RoundingDP = 4;
        public ProductionGraph Graph { get; }
        public abstract string DisplayName { get; }
        public abstract IEnumerable<Item> Inputs { get; }
        public abstract IEnumerable<Item> Outputs { get; }
        public double SpeedBonus { get; internal set; }
        public double ProductivityBonus { get; set; }

        public List<NodeLink> InputLinks { get; } = new List<NodeLink>();
        public List<NodeLink> OutputLinks { get; } = new List<NodeLink>();
        public RateType RateType { get; set; } = RateType.Auto;

        // The rate the solver calculated is appropriate for this node.
        public float ActualRate { get; protected set; }

        // If the rateType is manual, this field contains the rate the user desires.
        public float DesiredRate { get; set; }

        // The calculated rate at which the given item is consumed by this node. This may not match
        // the desired amount!
        public abstract float GetConsumeRate(Item item);

        // The calculated rate at which the given item is consumed by this node. This may not match
        // the desired amount!
        public abstract float GetSupplyRate(Item item);

        protected ProductionNode(ProductionGraph graph)
        {
            Graph = graph;
        }

        public bool CanUltimatelyTakeFrom(ProductionNode node)
        {
            Queue<ProductionNode> Q = new Queue<ProductionNode>();
            HashSet<ProductionNode> V = new HashSet<ProductionNode>();

            V.Add(this);
            Q.Enqueue(this);

            while (Q.Any()) {
                ProductionNode t = Q.Dequeue();
                if (t == node) {
                    return true;
                }
                foreach (NodeLink e in t.InputLinks) {
                    ProductionNode u = e.Supplier;
                    if (!V.Contains(u)) {
                        V.Add(u);
                        Q.Enqueue(u);
                    }
                }
            }
            return false;
        }

        public void Destroy()
        {
            foreach (NodeLink link in InputLinks.ToList().Union(OutputLinks.ToList())) {
                link.Destroy();
            }
            Graph.Nodes.Remove(this);
            Graph.InvalidateCaches();
        }

        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

        public virtual float ProductivityMultiplier()
        {
            return (float)(1.0 + ProductivityBonus);
        }

        internal double GetProductivityBonus()
        {
            return 1.5;
        }

        public float GetSuppliedRate(Item item)
        {
            return (float)InputLinks.Where(x => x.Item == item).Sum(x => x.Throughput);
        }

        internal bool OverSupplied(Item item)
        {
            return (Math.Round(GetConsumeRate(item), 2) < Math.Round(GetSuppliedRate(item), 2));
        }

        internal bool ManualRateNotMet()
        {
            // TODO: Hard-coded epsilon is gross :(
            return RateType == RateType.Manual && Math.Abs(ActualRate - DesiredRate) > 0.0001;
        }
    }

    public class RecipeNode : ProductionNode
    {
        public Recipe BaseRecipe { get; }

        // If null, best available assembler should be used, using speed (not number of module slots)
        // as a proxy since "best" can have varying definitions.
        public Assembler Assembler { get; set; }

        public ModuleSelector Modules { get; set; }

        internal Dictionary<MachinePermutation, double> GetAssemblers()
        {
            var assembler = Assembler;

            if (assembler == null) {
                assembler = DataCache.Assemblers.Values
                    .Where(a => a.Enabled)
                    .Where(a => a.Categories.Contains(BaseRecipe.Category))
                    .Where(a => a.MaxIngredients >= BaseRecipe.Ingredients.Count)
                    .OrderBy(a => -a.Speed)
                    .FirstOrDefault();
            }

            var ret = new Dictionary<MachinePermutation, double>();

            if (assembler != null) {
                var modules = Modules.For(BaseRecipe, assembler.ModuleSlots);
                var required = ActualRate / assembler.GetRate(BaseRecipe.Time, (float)SpeedBonus, modules);
                ret.Add(new MachinePermutation(assembler, modules.ToList()), required);
            }

            return ret;
        }

        protected RecipeNode(Recipe baseRecipe, ProductionGraph graph)
            : base(graph)
        {
            BaseRecipe = baseRecipe;
            Modules = ModuleSelector.Fastest;
        }

        public override IEnumerable<Item> Inputs => BaseRecipe.Ingredients.Keys;

        public override IEnumerable<Item> Outputs => BaseRecipe.Results.Keys;

        public static RecipeNode Create(Recipe baseRecipe, ProductionGraph graph)
        {
            RecipeNode node = new RecipeNode(baseRecipe, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        //If the graph is showing amounts rather than rates, round up all fractions (because it doesn't make sense to do half a recipe, for example)
        private float ValidateRecipeRate(float amount)
        {
            if (Graph.SelectedAmountType == AmountType.FixedAmount) {
                return
                    (float)Math.Ceiling(Math.Round(amount,
                        RoundingDP)); //Subtracting a very small number stops the amount from getting rounded up due to FP errors. It's a bit hacky but it works for now.
            }
            return (float)Math.Round(amount, RoundingDP);
        }

        public override string DisplayName => BaseRecipe.FriendlyName;

        public override string ToString()
        {
            return $"Recipe Tree Node: {BaseRecipe.Name}";
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "Recipe");
            info.AddValue("RecipeName", BaseRecipe.Name);
            info.AddValue("SpeedBonus", SpeedBonus);
            info.AddValue("ProductivityBonus", ProductivityBonus);
            info.AddValue("RateType", RateType);
            info.AddValue("ActualRate", ActualRate);
            if (RateType == RateType.Manual) {
                info.AddValue("DesiredRate", DesiredRate);
            }
            if (Assembler != null) {
                info.AddValue("Assembler", Assembler.Name);
            }
            Modules.GetObjectData(info, context);
        }

        public override float GetConsumeRate(Item item)
        {
            if (BaseRecipe.IsMissingRecipe
                || !BaseRecipe.Ingredients.ContainsKey(item)) {
                return 0f;
            }

            return (float)Math.Round(BaseRecipe.Ingredients[item] * ActualRate, RoundingDP);
        }

        public override float GetSupplyRate(Item item)
        {
            if (BaseRecipe.IsMissingRecipe
                || !BaseRecipe.Results.ContainsKey(item)) {
                return 0f;
            }

            return (float)Math.Round(BaseRecipe.Results[item] * ActualRate * ProductivityMultiplier(), RoundingDP);
        }

        internal override double OutputRateFor(Item item)
        {
            return BaseRecipe.Results[item];
        }

        internal override double InputRateFor(Item item)
        {
            return BaseRecipe.Ingredients[item];
        }

        public override float ProductivityMultiplier()
        {
            var assemblerBonus = GetAssemblers().Keys.Sum(x => x.GetAssemblerProductivity());
            return (float)(1.0 + ProductivityBonus + assemblerBonus);
        }
    }

    public class SupplyNode : ProductionNode
    {
        public Item SuppliedItem { get; }

        protected SupplyNode(Item item, ProductionGraph graph)
            : base(graph)
        {
            SuppliedItem = item;
        }

        public override IEnumerable<Item> Inputs => new List<Item>();

        public override IEnumerable<Item> Outputs
        {
            get { yield return SuppliedItem; }
        }

        public static SupplyNode Create(Item item, ProductionGraph graph)
        {
            SupplyNode node = new SupplyNode(item, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        public override string DisplayName => SuppliedItem.FriendlyName;

        public Dictionary<MachinePermutation, double> GetMinimumMiners()
        {
            Dictionary<MachinePermutation, double> results = new Dictionary<MachinePermutation, double>();

            Resource resource = DataCache.Resources.Values.FirstOrDefault(r => r.Result == SuppliedItem.Name);
            if (resource == null) {
                return results;
            }

            List<Miner> allowedMiners = DataCache.Miners.Values
                .Where(m => m.Enabled)
                .Where(m => m.ResourceCategories.Contains(resource.Category)).ToList();

            List<MachinePermutation> allowedPermutations = new List<MachinePermutation>();
            foreach (Miner miner in allowedMiners) {
                // TODO: Find correct recipe to pass in here. Needed to disallow productivity modules.
                allowedPermutations.AddRange(miner.GetAllPermutations(null));
            }

            List<MachinePermutation> sortedPermutations =
                allowedPermutations.OrderBy(p => p.GetMinerRate(resource)).ToList();

            if (sortedPermutations.Any()) {
                float requiredRate = GetSupplyRate(SuppliedItem);
                MachinePermutation permutationToAdd =
                    sortedPermutations.LastOrDefault(a => a.GetMinerRate(resource) < requiredRate);
                if (permutationToAdd != null) {
                    double numberToAdd = requiredRate / permutationToAdd.GetMinerRate(resource);
                    results.Add(permutationToAdd, numberToAdd);
                }
            }

            return results;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "Supply");
            info.AddValue("ItemName", SuppliedItem.Name);
            info.AddValue("RateType", RateType);
            info.AddValue("ActualRate", ActualRate);
            if (RateType == RateType.Manual) {
                info.AddValue("DesiredRate", DesiredRate);
            }
        }

        public override float GetConsumeRate(Item item)
        {
            Trace.Fail(string.Format("{0} supplier does not consume {1}, nothing should be asking for the rate!",
                SuppliedItem.FriendlyName, item.FriendlyName));
            return 0;
        }

        public override float GetSupplyRate(Item item)
        {
            if (SuppliedItem != item)
                Trace.Fail(string.Format("{0} supplier does not supply {1}, nothing should be asking for the rate!",
                    SuppliedItem.FriendlyName, item.FriendlyName));

            return (float)Math.Round(ActualRate, RoundingDP);
        }

        internal override double OutputRateFor(Item item)
        {
            return 1;
        }

        internal override double InputRateFor(Item item)
        {
            throw new ArgumentException("Supply node should not have any inputs!");
        }
    }

    public class ConsumerNode : ProductionNode
    {
        public Item ConsumedItem { get; }

        public override string DisplayName => ConsumedItem.FriendlyName;

        public override IEnumerable<Item> Inputs
        {
            get { yield return ConsumedItem; }
        }

        public override IEnumerable<Item> Outputs => new List<Item>();

        protected ConsumerNode(Item item, ProductionGraph graph)
            : base(graph)
        {
            ConsumedItem = item;
            RateType = RateType.Manual;
            ActualRate = 1f;
        }

        public static ConsumerNode Create(Item item, ProductionGraph graph)
        {
            ConsumerNode node = new ConsumerNode(item, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "Consumer");
            info.AddValue("ItemName", ConsumedItem.Name);
            info.AddValue("RateType", RateType);
            info.AddValue("ActualRate", ActualRate);
            if (RateType == RateType.Manual) {
                info.AddValue("DesiredRate", DesiredRate);
            }
        }

        public override float GetConsumeRate(Item item)
        {
            if (ConsumedItem != item)
                Trace.Fail(string.Format("{0} consumer does not consume {1}, nothing should be asking for the rate!",
                    ConsumedItem.FriendlyName, item.FriendlyName));

            return (float)Math.Round(ActualRate, RoundingDP);
        }

        public override float GetSupplyRate(Item item)
        {
            Trace.Fail(string.Format("{0} consumer does not supply {1}, nothing should be asking for the rate!",
                ConsumedItem.FriendlyName, item.FriendlyName));

            return 0;
        }

        internal override double OutputRateFor(Item item)
        {
            throw new ArgumentException("Consumer should not have outputs!");
        }

        internal override double InputRateFor(Item item)
        {
            return 1;
        }
    }

    public class PassthroughNode : ProductionNode
    {
        public Item PassedItem { get; }

        protected PassthroughNode(Item item, ProductionGraph graph)
            : base(graph)
        {
            PassedItem = item;
        }

        public override IEnumerable<Item> Inputs => Enumerable.Repeat(PassedItem, 1);

        public override IEnumerable<Item> Outputs => Inputs;

        public static PassthroughNode Create(Item item, ProductionGraph graph)
        {
            PassthroughNode node = new PassthroughNode(item, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        //If the graph is showing amounts rather than rates, round up all fractions (because it doesn't make sense to do half a recipe, for example)
        private float ValidateRecipeRate(float amount)
        {
            if (Graph.SelectedAmountType == AmountType.FixedAmount) {
                return
                    (float)Math.Ceiling(Math.Round(amount,
                        RoundingDP)); //Subtracting a very small number stops the amount from getting rounded up due to FP errors. It's a bit hacky but it works for now.
            }
            return (float)Math.Round(amount, RoundingDP);
        }

        public override string DisplayName => PassedItem.FriendlyName;

        public override string ToString()
        {
            return $"Pass-through Tree Node: {PassedItem.Name}";
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "PassThrough");
            info.AddValue("ItemName", PassedItem.Name);
            info.AddValue("RateType", RateType);
            info.AddValue("ActualRate", ActualRate);
            if (RateType == RateType.Manual) {
                info.AddValue("DesiredRate", DesiredRate);
            }
        }

        public override float GetConsumeRate(Item item)
        {
            return (float)Math.Round(ActualRate, RoundingDP);
        }

        public override float GetSupplyRate(Item item)
        {
            return (float)Math.Round(ActualRate, RoundingDP);
        }

        internal override double OutputRateFor(Item item)
        {
            return 1;
        }

        internal override double InputRateFor(Item item)
        {
            return 1;
        }

        public override float ProductivityMultiplier()
        {
            return 1;
        }
    }
}
