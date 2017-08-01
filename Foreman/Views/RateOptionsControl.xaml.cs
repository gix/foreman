namespace Foreman.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Controls;

    public partial class RateOptionsControl
    {
        private readonly ProductionGraphViewModel graphViewModel;
        private readonly ProductionGraph graph;
        private readonly Button[] moduleButtons;

        private ProductionNode BaseNode { get; }

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

            productivityBonusTextBox.Text = Convert.ToString(BaseNode.ProductivityBonus);
            speedBonusTextBox.Text = Convert.ToString(BaseNode.SpeedBonus);

            if (graphViewModel.ShowAssemblers && BaseNode is RecipeNode) {
                assemblerPanel.Visibility = Visibility.Visible;
                UpdateAssemblerButtons();
            } else {
                assemblerPanel.Visibility = Visibility.Collapsed;
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
                    button.Content = moduleSet[i]?.Icon;
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

            var allowedAssemblers = DataCache.Assemblers.Values
                .Where(a => a.Enabled)
                .Where(a => a.Categories.Contains(recipe.Category))
                .Where(a => a.MaxIngredients >= recipe.Ingredients.Count);
            foreach (var assembler in allowedAssemblers.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Items.Values.SingleOrDefault(i => i.Name == assembler.Name);
                optionList.Add(new ItemChoice(item, assembler.FriendlyName, assembler.FriendlyName));
            }

            var c = await optionList.ChooseAsync(assemblerButton, PlacementMode.Right);
            if (c != null) {
                if (c == bestOption) {
                    recipeNode.Assembler = null;
                } else {
                    var assembler = DataCache.Assemblers.Single(a => a.Key == c.DisplayText).Value;
                    recipeNode.Assembler = assembler;
                }
                UpdateAssemblerButtons();
                graph.UpdateNodeValues();
            }
        }

        private async void modulesButton_Click(object sender, EventArgs e)
        {
            var optionList = new List<Choice>();
            var fastestOption = new ItemChoice(null, "Best", "Best");
            optionList.Add(fastestOption);

            var noneOption = new ItemChoice(null, "None", "None");
            optionList.Add(noneOption);

            var productivityOption = new ItemChoice(null, "Most Productive", "Most Productive");
            optionList.Add(productivityOption);

            var setOption = new ItemChoice(null, "Manual Set", "Manual Set");
            optionList.Add(setOption);

            var recipeNode = (RecipeNode)BaseNode;
            var recipe = recipeNode.BaseRecipe;

            var allowedModules = DataCache.Modules.Values
                .Where(a => a.Enabled)
                .Where(a => a.AllowedIn(recipe));

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChoice(item, module.FriendlyName, module.FriendlyName));
            }

            var c = await optionList.ChooseAsync(modulesButton, PlacementMode.Right);
            if (c != null) {
                if (c == fastestOption) {
                    recipeNode.Modules = ModuleSelector.Fastest;
                } else if (c == noneOption) {
                    recipeNode.Modules = ModuleSelector.None;
                } else if (c == productivityOption) {
                    recipeNode.Modules = ModuleSelector.Productive;
                } else if (c == setOption) {
                    recipeNode.Modules = new ModuleSet();
                } else {
                    var module = DataCache.Modules.Single(a => a.Key == c.DisplayText).Value;
                    recipeNode.Modules = ModuleSelector.Specific(module);
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

            var allowedModules = DataCache.Modules.Values
                .Where(x => x.Enabled)
                .Where(x => x.AllowedIn(recipeNode.BaseRecipe));

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChoice(item, module.FriendlyName, module.FriendlyName));
            }

            var c = await optionList.ChooseAsync(button, PlacementMode.Right);
            if (c != null) {
                if (c == noneOption)
                    modules[moduleIndex] = null;
                else
                    modules[moduleIndex] = DataCache.Modules.Single(a => a.Key == c.DisplayText).Value;

                UpdateAssemblerButtons();
                graph.UpdateNodeValues();
            }
        }

        private void productivityBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            var input = (TextBox)sender;
            if (double.TryParse(input.Text, out double newAmount)) {
                BaseNode.ProductivityBonus = newAmount;
                graph.UpdateNodeValues();
            }
        }

        private void speedBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            var input = (TextBox)sender;
            if (double.TryParse(input.Text, out double newAmount)) {
                BaseNode.SpeedBonus = newAmount;
                graph.UpdateNodeValues();
            }
        }
    }
}
