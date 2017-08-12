namespace Foreman.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using Infrastructure;

    public class NodeOptionsViewModel : ViewModel
    {
        private readonly BindableCollection<ModuleSlot> modules =
            new BindableCollection<ModuleSlot>();
        private readonly BeaconModuleList beaconModules =
            new BeaconModuleList(true);

        private bool canEditAssembler;
        private RateType rateType;
        private double amount;
        private string amountUnit;

        private double speedBonus;
        private double productivityBonus;
        private double consumptionBonus;
        private bool canOverrideBonus;
        private object assembler;
        private object moduleStrategy;

        public NodeOptionsViewModel(
            ProductionNode baseNode, ProductionGraphViewModel graphViewModel)
        {
            BaseNode = baseNode;
            GraphViewModel = graphViewModel;
            Graph = graphViewModel.Graph;
            RateType = BaseNode.RateType;

            modules.ItemPropertyChanged += OnModuleSlotPropertyChanged;
            beaconModules.ItemPropertyChanged += OnModuleSlotPropertyChanged;

            float amountToShow = BaseNode.DesiredRate;
            switch (Graph.SelectedAmountType) {
                case AmountType.Rate:
                    switch (Graph.SelectedUnit) {
                        case RateUnit.PerSecond:
                            AmountUnit = "/s";
                            break;
                        case RateUnit.PerMinute:
                            AmountUnit = "/m";
                            amountToShow *= 60;
                            break;
                    }
                    break;
                case AmountType.FixedAmount:
                    AmountUnit = null;
                    break;
            }

            Amount = amountToShow;

            var moduleBag = BaseNode.BeaconModules;
            if (moduleBag.OverrideSpeedBonus == null &&
                moduleBag.OverrideProductivityBonus == null &&
                moduleBag.OverrideConsumptionBonus == null) {
                SpeedBonus = moduleBag.GetSpeedBonus();
                ProductivityBonus = moduleBag.GetProductivityBonus();
                ConsumptionBonus = moduleBag.GetConsumptionBonus();
            } else {
                CanOverrideBonus = true;
                SpeedBonus = moduleBag.OverrideSpeedBonus ?? 0;
                ProductivityBonus = moduleBag.OverrideProductivityBonus ?? 0;
                ConsumptionBonus = moduleBag.OverrideConsumptionBonus ?? 0;
            }

            foreach (var entry in moduleBag)
                beaconModules.Add(new ModuleSlot(entry.Module, entry.Count));

            if (GraphViewModel.ShowAssemblers && BaseNode is RecipeNode ||
                GraphViewModel.ShowMiners && BaseNode is SupplyNode) {
                CanEditAssembler = true;
                UpdateAssemblerButtons();
            }
        }

        public ProductionGraph Graph { get; }
        public ProductionNode BaseNode { get; }
        public ProductionGraphViewModel GraphViewModel { get; }
        public IReadOnlyList<ModuleSlot> Modules => modules;
        public IReadOnlyList<ModuleSlot> BeaconModules => beaconModules;

        public bool CanEditAssembler
        {
            get => canEditAssembler;
            set => SetProperty(ref canEditAssembler, value);
        }

        public RateType RateType
        {
            get => rateType;
            set
            {
                if (SetProperty(ref rateType, value)) {
                    BaseNode.RateType = value;
                    Graph.UpdateNodeValues();
                }
            }
        }

        public double Amount
        {
            get => amount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Amount must be positive.");

                if (SetProperty(ref amount, value)) {
                    var newAmount = value;
                    if (Graph.SelectedAmountType == AmountType.Rate &&
                        Graph.SelectedUnit == RateUnit.PerMinute) {
                        newAmount /= 60;
                    }

                    BaseNode.DesiredRate = (float)newAmount;
                    Graph.UpdateNodeValues();
                }
            }
        }

        public string AmountUnit
        {
            get => amountUnit;
            set => SetProperty(ref amountUnit, value);
        }

        public double SpeedBonus
        {
            get => speedBonus;
            set
            {
                if (SetProperty(ref speedBonus, value)) {
                    if (CanOverrideBonus) {
                        BaseNode.BeaconModules.OverrideSpeedBonus = value;
                        Graph.UpdateNodeValues();
                    }
                }
            }
        }

        public double ProductivityBonus
        {
            get => productivityBonus;
            set
            {
                if (SetProperty(ref productivityBonus, value)) {
                    if (CanOverrideBonus) {
                        BaseNode.BeaconModules.OverrideProductivityBonus = value;
                        Graph.UpdateNodeValues();
                    }
                }
            }
        }

        public double ConsumptionBonus
        {
            get => consumptionBonus;
            set
            {
                if (SetProperty(ref consumptionBonus, value)) {
                    if (CanOverrideBonus) {
                        BaseNode.BeaconModules.OverrideConsumptionBonus = value;
                        Graph.UpdateNodeValues();
                    }
                }
            }
        }

        public bool CanOverrideBonus
        {
            get => canOverrideBonus;
            set => SetProperty(ref canOverrideBonus, value);
        }

        public object Assembler
        {
            get => assembler;
            set => SetProperty(ref assembler, value);
        }

        public object ModuleStrategy
        {
            get => moduleStrategy;
            set => SetProperty(ref moduleStrategy, value);
        }

        public void UpdateAssemblerButtons()
        {
            if (!(BaseNode is EffectableNode node))
                return;

            var entity = node.ProductionEntity;
            Assembler = entity?.FriendlyName ?? "Best";
            ModuleStrategy = node.Modules.Name;

            modules.Clear();
            if (entity != null && node.Modules is ModuleSet moduleSet) {
                for (int i = 0; i < entity.ModuleSlots; ++i)
                    modules.Add(new ModuleSlot(moduleSet[i]));
            }
        }

        private void OnModuleSlotPropertyChanged(
            ModuleSlot item, PropertyChangedEventArgs args)
        {
            UpdateModules();
        }

        private void UpdateModules()
        {
            if (BaseNode is EffectableNode node && node.Modules is ModuleSet moduleSet) {
                moduleSet.Resize(modules.Count);
                for (int i = 0; i < modules.Count; ++i)
                    moduleSet[i] = modules[i].Module;
            }

            var nodeBeaconModules = BaseNode.BeaconModules;
            nodeBeaconModules.Clear();
            foreach (var entry in BeaconModules.Where(x => x.Module != null))
                nodeBeaconModules.Add(entry.Module, entry.Count);

            SpeedBonus = nodeBeaconModules.GetSpeedBonus();
            ProductivityBonus = nodeBeaconModules.GetProductivityBonus();
            ConsumptionBonus = nodeBeaconModules.GetConsumptionBonus();
            Graph.UpdateNodeValues();
        }

        private class BeaconModuleList : BindableCollection<ModuleSlot>
        {
            private readonly bool usesAggregatedSlots;
            private bool suppressCleanup;

            public BeaconModuleList(bool usesAggregatedSlots)
            {
                this.usesAggregatedSlots = usesAggregatedSlots;
                CleanupAndEnsureTail();
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                base.OnCollectionChanged(e);
                CleanupAndEnsureTail();
            }

            protected override void OnItemPropertyChanged(
                ModuleSlot item, PropertyChangedEventArgs args)
            {
                base.OnItemPropertyChanged(item, args);

                if (args.PropertyName == nameof(ModuleSlot.Module) && item.Module == null)
                    Remove(item);

                CleanupAndEnsureTail();
            }

            private static bool IsEmptyItem(ModuleSlot item)
            {
                return item == null || (item.Module == null && item.Count == 0);
            }

            private void CleanupAndEnsureTail()
            {
                if (suppressCleanup)
                    return;

                suppressCleanup = true;
                try {
                    for (int i = Count - 1; i >= 0; --i) {
                        if (IsEmptyItem(this[i]))
                            RemoveAt(i);
                    }

                    if (Count == 0 || !IsEmptyItem(this[Count - 1]))
                        Add(new ModuleSlot(usesAggregatedSlots));
                } finally {
                    suppressCleanup = false;
                }
            }
        }

        public async Task ChooseAssembler(UIElement placementTarget)
        {
            if (!(BaseNode is EffectableNode node))
                return;

            var optionList = new List<Choice>();
            var bestOption = new ItemChoice(null, "Best", "Best");
            optionList.Add(bestOption);

            foreach (var entity in node.GetAllowedProductionEntities().OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == entity.Name);
                optionList.Add(new ItemChoice(item, entity.FriendlyName, value: entity));
            }

            var c = await optionList.ChooseAsync(placementTarget, PlacementMode.Right);
            if (c != null) {
                if (c == bestOption) {
                    node.ProductionEntity = null;
                } else {
                    node.ProductionEntity = (ProductionEntity)c.Value;
                }
                UpdateAssemblerButtons();
                Graph.UpdateNodeValues();
            }
        }

        public async Task ChooseModuleSelector(UIElement placementTarget)
        {
            if (!(BaseNode is EffectableNode node))
                return;

            var noneOption = new ItemChoice(null, "None", "None");
            var fastestOption = new ItemChoice(null, "Fastest");
            var mostProductiveOption = new ItemChoice(null, "Most Productive");
            var mostEfficientOption = new ItemChoice(null, "Most Efficient");
            var customOption = new ItemChoice(null, "Custom");

            var allowedModules = DataCache.Current.Modules.Values
                .Where(a => a.Enabled && node.IsEffectableBy(a));

            var options = new List<Choice> {
                noneOption,
                fastestOption,
                mostProductiveOption,
                mostEfficientOption,
                customOption
            };

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                options.Add(new ItemChoice(item, module.FriendlyName, null, module));
            }

            var c = await options.ChooseAsync(placementTarget, PlacementMode.Right);
            if (c != null) {
                if (c == fastestOption)
                    node.Modules = ModuleSelector.Fastest;
                else if (c == noneOption)
                    node.Modules = ModuleSelector.None;
                else if (c == mostProductiveOption)
                    node.Modules = ModuleSelector.Productive;
                else if (c == mostEfficientOption)
                    node.Modules = ModuleSelector.Efficient;
                else if (c == customOption)
                    node.Modules = new ModuleSet();
                else
                    node.Modules = ModuleSelector.Specific((Module)c.Value);

                UpdateAssemblerButtons();
                Graph.UpdateNodeValues();
            }
        }

        public async Task ChooseModule(ModuleSlot slot, UIElement placementTarget)
        {
            if (!(BaseNode is EffectableNode node))
                return;

            var optionList = new List<Choice>();

            var noneOption = new ItemChoice(null, "None", "None");
            optionList.Add(noneOption);

            var allowedModules = DataCache.Current.Modules.Values
                .Where(x => x.Enabled && node.IsEffectableBy(x));
            if (slot.IsAggregated) {
                var beacon = DataCache.Current.Beacons.Values.First();
                allowedModules = allowedModules.Where(beacon.Allows);
            }

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChoice(item, module.FriendlyName, value: module));
            }

            var c = await optionList.ChooseAsync(placementTarget, PlacementMode.Right);
            if (c != null)
                slot.Module = c.Value as Module;
        }
    }

    public class ModuleSlot : ViewModel
    {
        private Module module;
        private int count;

        public ModuleSlot(bool isAggregated)
        {
            IsAggregated = isAggregated;
        }

        public ModuleSlot(Module module)
        {
            this.module = module;
            IsAggregated = false;
        }

        public ModuleSlot(Module module, int count)
        {
            this.module = module;
            this.count = count;
            IsAggregated = true;
        }

        public bool IsAggregated { get; }

        public Module Module
        {
            get => module;
            set => SetProperty(ref module, value);
        }

        public int Count
        {
            get => count;
            set => SetProperty(ref count, value);
        }
    }
}
