namespace Foreman.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Infrastructure;

    public partial class RateOptionsControl
    {
        private readonly ProductionGraphViewModel graphViewModel;
        private readonly ProductionGraph graph;
        private readonly Button[] moduleButtons;

        private ProductionNode BaseNode { get; }

        private readonly BeaconModuleList beaconModules =
            new BeaconModuleList();

        public RateOptionsControl()
        {
            InitializeComponent();

            moduleButtons = new[] {
                moduleButton1, moduleButton2, moduleButton3, moduleButton4
            };
        }

        public RateOptionsControl(ProductionNode baseNode, ProductionGraphViewModel graphViewModel)
            : this()
        {
            BaseNode = baseNode;
            this.graphViewModel = graphViewModel;
            graph = graphViewModel.Graph;
            beaconModulesList.ItemsSource = beaconModules;

            Initialize();
        }

        private void Initialize()
        {
            if (BaseNode.RateType == RateType.Auto) {
                autoOption.IsChecked = true;
                fixedTextBox.IsEnabled = false;
            } else {
                fixedOption.IsChecked = true;
                fixedTextBox.IsEnabled = true;
            }

            float amountToShow = BaseNode.DesiredRate;
            if (graph.SelectedAmountType == AmountType.Rate) {
                fixedTextBox.Width = 65;
                unitLabel.Visibility = Visibility.Visible;

                if (graph.SelectedUnit == RateUnit.PerMinute) {
                    amountToShow *= 60;
                    unitLabel.Text = "/m";
                } else {
                    unitLabel.Text = "/s";
                }
            } else {
                unitLabel.Visibility = Visibility.Collapsed;
                fixedTextBox.Width = 85;
            }
            fixedTextBox.Text = Convert.ToString(amountToShow);

            foreach (var entry in BaseNode.BeaconModules) {
                beaconModules.Add(new AggregatedModule(entry.Key, entry.Value));
            }

            speedBonusTextBox.Text = Convert.ToString(BaseNode.BeaconModules.GetSpeedBonus());
            productivityBonusTextBox.Text = Convert.ToString(BaseNode.BeaconModules.GetProductivityBonus());
            consumptionBonusTextBox.Text = Convert.ToString(BaseNode.BeaconModules.GetSpeedBonus());

            speedBonusTextBox.IsReadOnly = BaseNode.BeaconModules.OverrideSpeedBonus == null;
            productivityBonusTextBox.IsReadOnly = BaseNode.BeaconModules.OverrideProductivityBonus == null;
            consumptionBonusTextBox.IsReadOnly = BaseNode.BeaconModules.OverrideConsumptionBonus == null;

            if (graphViewModel.ShowAssemblers && BaseNode is RecipeNode) {
                assemblerPanel.Visibility = Visibility.Visible;
                UpdateAssemblerButtons();
            } else {
                assemblerPanel.Visibility = Visibility.Collapsed;
            }
        }

        private class BeaconModuleList : BindableCollection<AggregatedModule>
        {
            private bool suppressCleanup;

            public BeaconModuleList()
            {
                CleanupAndEnsureTail();
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                base.OnCollectionChanged(e);
                CleanupAndEnsureTail();
            }

            protected override void OnItemPropertyChanged(
                AggregatedModule item, PropertyChangedEventArgs args)
            {
                base.OnItemPropertyChanged(item, args);

                if (args.PropertyName == nameof(AggregatedModule.Module) && item.Module == null)
                    Remove(item);

                CleanupAndEnsureTail();
            }

            private static bool IsEmptyItem(AggregatedModule item)
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
                        Add(new AggregatedModule());
                } finally {
                    suppressCleanup = false;
                }
            }
        }

        private void UpdateAssemblerButtons()
        {
            var recipeNode = (RecipeNode)BaseNode;
            var assembler = recipeNode.Assembler;
            if (assembler == null) {
                assemblerButton.Content = "Best";
            } else {
                assemblerButton.Content = assembler.FriendlyName;
            }

            modulesButton.Content = recipeNode.Modules.Name;

            var moduleSet = recipeNode.Modules as ModuleSet;
            var moduleSlots = assembler != null && moduleSet != null ? assembler.ModuleSlots : 0;
            for (int i = 0; i < moduleButtons.Length; ++i) {
                var button = moduleButtons[i];
                if (i < moduleSlots) {
                    button.IsEnabled = true;
                    button.Content = moduleSet[i];
                } else {
                    button.IsEnabled = false;
                    button.Content = null;
                }
            }
        }

        private void fixedOption_CheckedChanged(object sender, EventArgs e)
        {
            fixedTextBox.IsEnabled = fixedOption.IsChecked ?? false;
            if (fixedTextBox.IsEnabled) {
                BaseNode.RateType = RateType.Manual;
            } else {
                BaseNode.RateType = RateType.Auto;
            }
            graph.UpdateNodeValues();
        }

        private void fixedTextBox_TextChanged(object sender, EventArgs e)
        {
            float newAmount;
            if (float.TryParse(fixedTextBox.Text, out newAmount)) {
                if (graph.SelectedAmountType == AmountType.Rate &&
                    graph.SelectedUnit == RateUnit.PerMinute) {
                    newAmount /= 60;
                }
                BaseNode.DesiredRate = newAmount;
                graph.UpdateNodeValues();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Enter)
                graph.UpdateNodeValues();
        }

        private async void assemblerButton_Click(object sender, EventArgs e)
        {
            var optionList = new List<Choice>();
            var bestOption = new ItemChoice(null, "Best", "Best");
            optionList.Add(bestOption);

            var recipeNode = (RecipeNode)BaseNode;
            var recipe = recipeNode.BaseRecipe;

            var allowedAssemblers = DataCache.Current.Assemblers.Values
                .Where(a => a.Enabled)
                .Where(a => a.Categories.Contains(recipe.Category))
                .Where(a => a.MaxIngredients >= recipe.Ingredients.Count);
            foreach (var assembler in allowedAssemblers.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == assembler.Name);
                optionList.Add(new ItemChoice(item, assembler.FriendlyName, assembler.FriendlyName, assembler));
            }

            var c = await optionList.ChooseAsync(assemblerButton, PlacementMode.Right);
            if (c != null) {
                if (c == bestOption) {
                    recipeNode.Assembler = null;
                } else {
                    recipeNode.Assembler = (Assembler)c.Value;
                }
                UpdateAssemblerButtons();
                graph.UpdateNodeValues();
            }
        }

        private async void modulesButton_Click(object sender, EventArgs e)
        {
            var noneOption = new ItemChoice(null, "None", "None");
            var fastestOption = new ItemChoice(null, "Fastest");
            var mostProductiveOption = new ItemChoice(null, "Most Productive");
            var mostEfficientOption = new ItemChoice(null, "Most Efficient");
            var customOption = new ItemChoice(null, "Custom");

            var recipeNode = (RecipeNode)BaseNode;
            var recipe = recipeNode.BaseRecipe;

            var allowedModules = DataCache.Current.Modules.Values
                .Where(a => a.Enabled)
                .Where(a => a.AllowedIn(recipe));

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

            var c = await options.ChooseAsync(modulesButton, PlacementMode.Right);
            if (c != null) {
                if (c == fastestOption) {
                    recipeNode.Modules = ModuleSelector.Fastest;
                } else if (c == noneOption) {
                    recipeNode.Modules = ModuleSelector.None;
                } else if (c == mostProductiveOption) {
                    recipeNode.Modules = ModuleSelector.Productive;
                } else if (c == mostEfficientOption) {
                    recipeNode.Modules = ModuleSelector.Efficient;
                } else if (c == customOption) {
                    recipeNode.Modules = new ModuleSet();
                } else {
                    recipeNode.Modules = ModuleSelector.Specific((Module)c.Value);
                }
                UpdateAssemblerButtons();
                graph.UpdateNodeValues();
            }
        }

        private async void moduleButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var recipeNode = (RecipeNode)BaseNode;

            var assembler = recipeNode.Assembler;
            var modules = recipeNode.Modules as ModuleSet;
            int moduleIndex = Array.IndexOf(moduleButtons, button);

            if (modules == null || assembler == null || moduleIndex == -1)
                return;

            var optionList = new List<Choice>();

            var noneOption = new ItemChoice(null, "None", "None");
            optionList.Add(noneOption);

            var allowedModules = DataCache.Current.Modules.Values
                .Where(x => x.Enabled)
                .Where(x => x.AllowedIn(recipeNode.BaseRecipe));

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChoice(item, module.FriendlyName, module.FriendlyName, module));
            }

            var c = await optionList.ChooseAsync(button, PlacementMode.Right);
            if (c != null) {
                if (c == noneOption)
                    modules[moduleIndex] = null;
                else
                    modules[moduleIndex] = (Module)c.Value;

                UpdateAssemblerButtons();
                graph.UpdateNodeValues();
            }
        }

        private void speedBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            var input = (TextBox)sender;
            if (!input.IsReadOnly && double.TryParse(input.Text, out double newAmount)) {
                BaseNode.BeaconModules.OverrideSpeedBonus = newAmount;
                graph.UpdateNodeValues();
            }
        }

        private void productivityBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            var input = (TextBox)sender;
            if (!input.IsReadOnly && double.TryParse(input.Text, out double newAmount)) {
                BaseNode.BeaconModules.OverrideProductivityBonus = newAmount;
                graph.UpdateNodeValues();
            }
        }

        private void consumptionBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            var input = (TextBox)sender;
            if (!input.IsReadOnly && double.TryParse(input.Text, out double newAmount)) {
                BaseNode.BeaconModules.OverrideConsumptionBonus = newAmount;
                graph.UpdateNodeValues();
            }
        }

        private async void aggregatedModuleButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var aggregatedModule = (AggregatedModule)button.DataContext;
            var recipeNode = (RecipeNode)BaseNode;

            var optionList = new List<Choice>();

            var noneOption = new ItemChoice(null, "None", "None");
            optionList.Add(noneOption);

            var allowedModules = DataCache.Current.Modules.Values
                .Where(x => x.Enabled)
                .Where(x => x.AllowedIn(recipeNode.BaseRecipe));

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Current.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChoice(item, module.FriendlyName, module.FriendlyName, module));
            }

            var c = await optionList.ChooseAsync(button, PlacementMode.Right);
            if (c != null) {
                aggregatedModule.Module = c.Value as Module;
                UpdateBeaconModules();
            }
        }

        private void aggregatedModuleCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBeaconModules();
        }

        private void UpdateBeaconModules()
        {
            var recipeNode = (RecipeNode)BaseNode;
            var modules = recipeNode.BeaconModules;
            modules.Clear();
            foreach (var entry in beaconModules.Where(x => x.Module != null))
                modules.Add(entry.Module, entry.Count);

            speedBonusTextBox.Text = modules.GetSpeedBonus().ToString();
            productivityBonusTextBox.Text = modules.GetProductivityBonus().ToString();
            consumptionBonusTextBox.Text = modules.GetConsumptionBonus().ToString();
            graph.UpdateNodeValues();
        }
    }

    public class AggregatedModule : ViewModel
    {
        private Module module;
        private int count;

        public AggregatedModule()
        {
        }

        public AggregatedModule(Module module, int count)
        {
            this.module = module;
            this.count = count;
        }

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
