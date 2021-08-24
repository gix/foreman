namespace Foreman
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;

    public enum RateType
    {
        Auto,
        Manual,
        Count
    }

    public class ModuleBag : IReadOnlyCollection<(Module Module, int Count)>
    {
        private const double DistributionEfficiency = 2.0;
        private readonly Dictionary<Module, int> modules = new();

        public double? OverrideSpeedBonus { get; set; }
        public double? OverrideProductivityBonus { get; set; }
        public double? OverrideConsumptionBonus { get; set; }

        public double GetSpeedBonus()
        {
            return OverrideSpeedBonus ?? modules.Sum(x => x.Key.SpeedBonus * x.Value) / DistributionEfficiency;
        }

        public double GetProductivityBonus()
        {
            return OverrideProductivityBonus ?? modules.Sum(x => x.Key.ProductivityBonus * x.Value) / DistributionEfficiency;
        }

        public double GetConsumptionBonus()
        {
            return OverrideConsumptionBonus ?? modules.Sum(x => x.Key.ConsumptionBonus * x.Value) / DistributionEfficiency;
        }

        public int Count { get; private set; }

        public void Clear()
        {
            modules.Clear();
            Count = 0;
        }

        public void Add(Module module, int count = 1)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            modules.TryGetValue(module, out int thisCount);
            modules[module] = thisCount + count;
            ++Count;
        }

        public bool Remove(Module module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (modules.TryGetValue(module, out int count)) {
                if (count == 1)
                    modules.Remove(module);
                else
                    modules[module] = count - 1;

                --Count;
                return true;
            }

            return false;
        }

        public IEnumerator<(Module Module, int Count)> GetEnumerator()
        {
            foreach (var entry in modules)
                yield return (entry.Key, entry.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [Serializable]
    public abstract partial class ProductionNode : ISerializable
    {
        public static readonly int RoundingDP = 4;
        public ProductionGraph Graph { get; }
        public abstract string DisplayName { get; }
        public abstract IEnumerable<Item> Inputs { get; }
        public abstract IEnumerable<Item> Outputs { get; }
        public ModuleBag BeaconModules { get; } = new();

        public List<NodeLink> InputLinks { get; } = new();
        public List<NodeLink> OutputLinks { get; } = new();
        public RateType RateType { get; set; } = RateType.Auto;

        // The rate the solver calculated is appropriate for this node.
        public float ActualRate { get; protected set; }

        /// <summary>
        ///   If the <see cref="RateType"/> is <see cref="F:RateType.Manual"/>,
        ///   this field contains the rate the user desires.
        /// </summary>
        public float DesiredRate { get; set; }

        /// <summary>
        ///   If the <see cref="RateType"/> is <see cref="F:RateType.Count"/>,
        ///   this field contains the rate the user desires.
        /// </summary>
        public float DesiredCount { get; set; }

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

        public bool Supplies(Item? item)
        {
            return item != null && Outputs.Contains(item);
        }

        public bool Consumes(Item? item)
        {
            return item != null && Inputs.Contains(item);
        }

        public bool CanUltimatelyTakeFrom(ProductionNode node)
        {
            var Q = new Queue<ProductionNode>();
            var V = new HashSet<ProductionNode>();

            V.Add(this);
            Q.Enqueue(this);

            while (Q.Any()) {
                ProductionNode t = Q.Dequeue();
                if (t == node)
                    return true;

                foreach (NodeLink e in t.InputLinks) {
                    ProductionNode u = e.Supplier;
                    if (V.Add(u))
                        Q.Enqueue(u);
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

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RateType", RateType);
            info.AddValue("ActualRate", ActualRate);
            switch (RateType) {
                case RateType.Manual:
                    info.AddValue("DesiredRate", DesiredRate);
                    break;
                case RateType.Count:
                    info.AddValue("DesiredCount", DesiredCount);
                    break;
            }
        }

        public virtual float ProductivityMultiplier()
        {
            return (float)(1.0 + BeaconModules.GetProductivityBonus());
        }

        public float GetSuppliedRate(Item item)
        {
            return (float)InputLinks.Where(x => x.Item == item).Sum(x => x.Throughput);
        }

        public virtual float GetDesiredConsumeRate(Item item) => 0;

        public virtual double GetEffectiveProductionRate() => 1;

        public double? GetActualDesiredRate()
        {
            return RateType switch {
                RateType.Manual => DesiredRate,
                RateType.Count => DesiredCount * GetEffectiveProductionRate(),
                _ => null
            };
        }

        internal bool OverSupplied(Item item)
        {
            return Math.Round(GetConsumeRate(item), 2) < Math.Round(GetSuppliedRate(item), 2);
        }

        internal bool ManualRateNotMet()
        {
            double? rate = GetActualDesiredRate();

            // TODO: Hard-coded epsilon is gross :(
            return rate != null && Math.Abs(ActualRate - rate.Value) > 0.0001;
        }

        internal bool ManualRateNotMet(Item item)
        {
            // Ignore unconnected inputs here. They always have enough input.
            if (!InputLinks.Any(x => x.Item == item))
                return false;

            return Math.Round(GetDesiredConsumeRate(item), 2) > Math.Round(GetSuppliedRate(item), 2);
        }
    }

    public abstract class EffectableNode : ProductionNode
    {
        protected EffectableNode(ProductionGraph graph) : base(graph)
        {
            Modules = ModuleSelector.Default;
        }

        public ModuleSelector Modules { get; set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            Modules.GetObjectData(info, context);
        }

        public abstract bool IsEffectableBy(Module module);

        public abstract IEnumerable<ProductionEntity> GetAllowedProductionEntities();
        public abstract ProductionEntity? ProductionEntity { get; set; }

        public abstract override double GetEffectiveProductionRate();
    }

    [Serializable]
    public class RecipeNode : EffectableNode
    {
        public Recipe BaseRecipe { get; }

        // If null, best available assembler should be used, using speed (not number of module slots)
        // as a proxy since "best" can have varying definitions.
        public Assembler? Assembler { get; set; }

        public IEnumerable<Assembler> GetAllowedAssemblers()
        {
            return DataCache.Current.Assemblers.Values
                .Where(a => a.Enabled)
                .Where(a => a.Categories.Contains(BaseRecipe.Category))
                .Where(a => a.MaxIngredients >= BaseRecipe.Ingredients.Count);
        }

        public Assembler? GetEffectiveAssembler()
        {
            return Assembler ?? GetAllowedAssemblers()
                .OrderByDescending(a => a.Speed)
                .ThenByDescending(a => a.ModuleSlots)
                .FirstOrDefault();
        }

        public override double GetEffectiveProductionRate()
        {
            Assembler? assembler = GetEffectiveAssembler();
            if (assembler == null)
                return 0;

            var modules = Modules.For(assembler, BaseRecipe, assembler.ModuleSlots).ToList();
            return assembler.GetRate(
                BaseRecipe.Time, BeaconModules.GetSpeedBonus(), modules);
        }

        internal Dictionary<MachinePermutation, double> GetAssemblers()
        {
            Assembler? assembler = GetEffectiveAssembler();

            var results = new Dictionary<MachinePermutation, double>();

            if (assembler != null) {
                var modules = Modules.For(assembler, BaseRecipe, assembler.ModuleSlots).ToList();
                var required = ActualRate / assembler.GetRate(
                    BaseRecipe.Time, BeaconModules.GetSpeedBonus(), modules);
                results.Add(new MachinePermutation(assembler, modules), required);
            }

            return results;
        }

        protected RecipeNode(Recipe baseRecipe, ProductionGraph graph)
            : base(graph)
        {
            BaseRecipe = baseRecipe;
        }

        public override IEnumerable<Item> Inputs => BaseRecipe.Ingredients.Keys;

        public override IEnumerable<Item> Outputs => BaseRecipe.Results.Keys;

        public static RecipeNode Create(Recipe baseRecipe, ProductionGraph graph)
        {
            var node = new RecipeNode(baseRecipe, graph);
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
            base.GetObjectData(info, context);
            info.AddValue("BeaconModules", BeaconModules.ToDictionary(x => x.Module, x => x.Count));
            info.AddValue("SpeedBonus", BeaconModules.OverrideSpeedBonus);
            info.AddValue("ProductivityBonus", BeaconModules.OverrideProductivityBonus);
            info.AddValue("ConsumptionBonus", BeaconModules.OverrideConsumptionBonus);
            if (Assembler != null) {
                info.AddValue("Assembler", Assembler.Name);
            }
        }

        public override bool IsEffectableBy(Module module)
        {
            return module.AllowedIn(Assembler, BaseRecipe);
        }

        public override IEnumerable<ProductionEntity> GetAllowedProductionEntities()
        {
            return GetAllowedAssemblers();
        }

        public override ProductionEntity? ProductionEntity
        {
            get => Assembler;
            set => Assembler = (Assembler?)value;
        }

        public override float GetConsumeRate(Item item)
        {
            if (BaseRecipe.IsMissingRecipe
                || !BaseRecipe.Ingredients.ContainsKey(item)) {
                return 0f;
            }

            return (float)Math.Round(BaseRecipe.Ingredients[item] * ActualRate, RoundingDP);
        }

        public override float GetDesiredConsumeRate(Item item)
        {
            if (BaseRecipe.IsMissingRecipe
                || !BaseRecipe.Ingredients.ContainsKey(item)) {
                return 0f;
            }

            double? rate = GetActualDesiredRate();
            if (rate == null)
                return 0f;

            return (float)Math.Round(BaseRecipe.Ingredients[item] * rate.Value, RoundingDP);
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
            return (float)(1.0 + BeaconModules.GetProductivityBonus() + assemblerBonus);
        }

        public RecipeNode Clone(ProductionGraph graph)
        {
            var node = Create(BaseRecipe, graph);
            node.RateType = RateType;
            node.DesiredRate = DesiredRate;
            node.DesiredCount = DesiredCount;
            return node;
        }
    }

    [Serializable]
    public class SupplyNode : EffectableNode
    {
        private Resource? resource;

        public Item SuppliedItem { get; }

        public Resource? Resource =>
            resource ??= DataCache.Current.Resources.Values.FirstOrDefault(
                r => r.Result == SuppliedItem);

        protected SupplyNode(Item item, ProductionGraph graph)
            : base(graph)
        {
            SuppliedItem = item;
        }

        public override IEnumerable<Item> Inputs { get; } = new List<Item>();

        public override IEnumerable<Item> Outputs
        {
            get { yield return SuppliedItem; }
        }

        public static SupplyNode Create(Item item, ProductionGraph graph)
        {
            var node = new SupplyNode(item, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        public override string DisplayName => SuppliedItem.FriendlyName;
        public Miner? Miner { get; set; }

        public override IEnumerable<ProductionEntity> GetAllowedProductionEntities()
        {
            return GetAllowedAssemblers();
        }

        public override ProductionEntity? ProductionEntity
        {
            get => Miner;
            set => Miner = (Miner?)value;
        }

        public IEnumerable<Miner> GetAllowedAssemblers()
        {
            if (Resource == null)
                return Enumerable.Empty<Miner>();

            return DataCache.Current.Miners.Values
                .Where(a => a.Enabled)
                .Where(a => a.ResourceCategories.Contains(Resource.Category));
        }

        public Miner? GetEffectiveMiner()
        {
            return Miner ?? GetAllowedAssemblers()
                .OrderByDescending(a => a.Speed)
                .ThenByDescending(a => a.ModuleSlots)
                .FirstOrDefault();
        }

        public override double GetEffectiveProductionRate()
        {
            if (Resource == null)
                return 0;

            Miner? miner = GetEffectiveMiner();
            if (miner == null)
                return 0;

            var modules = Modules.For(miner, Resource, miner.ModuleSlots).ToList();
            return miner.GetRate(Resource, BeaconModules.GetSpeedBonus(), modules);
        }

        public Dictionary<MachinePermutation, double> GetMinimumMiners()
        {
            var results = new Dictionary<MachinePermutation, double>();
            if (Resource == null)
                return results;

            Miner? miner = GetEffectiveMiner();

            if (miner != null) {
                var modules = Modules.For(miner, Resource, miner.ModuleSlots).ToList();
                var permutation = new MachinePermutation(miner, modules);
                var required = ActualRate / miner.GetRate(Resource, BeaconModules.GetSpeedBonus(), modules);
                results.Add(permutation, required);
            }

            return results;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "Supply");
            info.AddValue("ItemName", SuppliedItem.Name);
            base.GetObjectData(info, context);
        }

        public override bool IsEffectableBy(Module module)
        {
            return module.AllowedIn(Miner, Resource);
        }

        public override float GetConsumeRate(Item item)
        {
            Trace.Fail(
                $"{SuppliedItem.FriendlyName} supplier does not consume {item.FriendlyName}, nothing should be asking for the rate!");
            return 0;
        }

        public override float GetSupplyRate(Item item)
        {
            if (SuppliedItem != item)
                Trace.Fail(
                    $"{SuppliedItem.FriendlyName} supplier does not supply {item.FriendlyName}, nothing should be asking for the rate!");

            return (float)Math.Round(ActualRate * ProductivityMultiplier(), RoundingDP);
        }

        internal override double OutputRateFor(Item item)
        {
            return 1;
        }

        internal override double InputRateFor(Item item)
        {
            throw new ArgumentException("Supply node should not have any inputs!");
        }

        public override float ProductivityMultiplier()
        {
            var assemblerBonus = GetMinimumMiners().Keys.Sum(x => x.GetAssemblerProductivity());
            return (float)(1.0 + BeaconModules.GetProductivityBonus() + assemblerBonus);
        }

        public SupplyNode Clone(ProductionGraph graph)
        {
            var node = Create(SuppliedItem, graph);
            node.RateType = RateType;
            node.DesiredRate = DesiredRate;
            node.DesiredCount = DesiredCount;
            return node;
        }
    }

    [Serializable]
    public class ConsumerNode : ProductionNode
    {
        public Item ConsumedItem { get; }

        public override string DisplayName => ConsumedItem.FriendlyName;

        public override IEnumerable<Item> Inputs
        {
            get { yield return ConsumedItem; }
        }

        public override IEnumerable<Item> Outputs { get; } = new List<Item>();

        protected ConsumerNode(Item item, ProductionGraph graph)
            : base(graph)
        {
            ConsumedItem = item;
            RateType = RateType.Manual;
            ActualRate = 1f;
        }

        public static ConsumerNode Create(Item item, ProductionGraph graph)
        {
            var node = new ConsumerNode(item, graph);
            node.Graph.Nodes.Add(node);
            node.Graph.InvalidateCaches();
            return node;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodeType", "Consumer");
            info.AddValue("ItemName", ConsumedItem.Name);
            base.GetObjectData(info, context);
        }

        public override float GetConsumeRate(Item item)
        {
            if (ConsumedItem != item)
                Trace.Fail(
                    $"{ConsumedItem.FriendlyName} consumer does not consume {item.FriendlyName}, nothing should be asking for the rate!");

            return (float)Math.Round(ActualRate, RoundingDP);
        }

        public override float GetSupplyRate(Item item)
        {
            Trace.Fail(
                $"{ConsumedItem.FriendlyName} consumer does not supply {item.FriendlyName}, nothing should be asking for the rate!");

            return 0;
        }

        public override float GetDesiredConsumeRate(Item item)
        {
            return (float)Math.Round(DesiredRate, RoundingDP);
        }

        internal override double OutputRateFor(Item item)
        {
            throw new ArgumentException("Consumer should not have outputs!");
        }

        internal override double InputRateFor(Item item)
        {
            return 1;
        }

        public ConsumerNode Clone(ProductionGraph graph)
        {
            var node = Create(ConsumedItem, graph);
            node.RateType = RateType;
            node.DesiredRate = DesiredRate;
            node.DesiredCount = DesiredCount;
            return node;
        }
    }

    [Serializable]
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
            var node = new PassthroughNode(item, graph);
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
            base.GetObjectData(info, context);
        }

        public override float GetConsumeRate(Item item)
        {
            return (float)Math.Round(ActualRate, RoundingDP);
        }

        public override float GetSupplyRate(Item item)
        {
            return (float)Math.Round(ActualRate, RoundingDP);
        }

        public override float GetDesiredConsumeRate(Item item)
        {
            return (float)Math.Round(DesiredRate, RoundingDP);
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

        public PassthroughNode Clone(ProductionGraph graph)
        {
            var node = Create(PassedItem, graph);
            node.RateType = RateType;
            node.DesiredRate = DesiredRate;
            node.DesiredCount = DesiredCount;
            return node;
        }
    }
}
