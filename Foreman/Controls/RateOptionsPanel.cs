namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public partial class RateOptionsPanel : UserControl
    {
        public ProductionNode BaseNode { get; }
        public ProductionGraphViewer GraphViewer { get; }

        public RateOptionsPanel(ProductionNode baseNode, ProductionGraphViewer graphViewer)
        {
            InitializeComponent();

            BaseNode = baseNode;
            GraphViewer = graphViewer;

            if (baseNode.RateType == RateType.Auto) {
                autoOption.Checked = true;
                fixedTextBox.Enabled = false;
            } else {
                fixedOption.Checked = true;
                fixedTextBox.Enabled = true;
            }

            float amountToShow = baseNode.DesiredRate;
            if (GraphViewer.Graph.SelectedAmountType == AmountType.Rate) {
                fixedTextBox.Width = 65;
                unitLabel.Visible = true;

                if (GraphViewer.Graph.SelectedUnit == RateUnit.PerMinute) {
                    amountToShow *= 60;
                    unitLabel.Text = "/m";
                } else {
                    unitLabel.Text = "/s";
                }
            } else {
                unitLabel.Visible = false;
                fixedTextBox.Width = 85;
            }
            fixedTextBox.Text = Convert.ToString(amountToShow);

            productivityBonusTextBox.Text = Convert.ToString(baseNode.ProductivityBonus);
            speedBonusTextBox.Text = Convert.ToString(baseNode.SpeedBonus);

            if (GraphViewer.ShowAssemblers && baseNode is RecipeNode) {
                assemblerPanel.Visible = true;
                UpdateAssemblerButtons();
            } else {
                assemblerPanel.Visible = false;
            }
        }

        private void UpdateAssemblerButtons()
        {
            var recipeNode = (RecipeNode)BaseNode;
            var assembler = recipeNode.Assembler;
            if (assembler == null) {
                assemblerButton.Text = "Best";
            } else {
                assemblerButton.Text = assembler.FriendlyName;
            }

            modulesButton.Text = recipeNode.NodeModules.Name;
        }

        private void fixedOption_CheckedChanged(object sender, EventArgs e)
        {
            fixedTextBox.Enabled = fixedOption.Checked;
            if (fixedOption.Checked) {
                BaseNode.RateType = RateType.Manual;
            } else {
                BaseNode.RateType = RateType.Auto;
            }
            GraphViewer.Graph.UpdateNodeValues();
            GraphViewer.UpdateNodes();
        }

        private void fixedTextBox_TextChanged(object sender, EventArgs e)
        {
            float newAmount;
            if (float.TryParse(fixedTextBox.Text, out newAmount)) {
                if (GraphViewer.Graph.SelectedAmountType == AmountType.Rate &&
                    GraphViewer.Graph.SelectedUnit == RateUnit.PerMinute) {
                    newAmount /= 60;
                }
                BaseNode.DesiredRate = newAmount;
                GraphViewer.Graph.UpdateNodeValues();
                GraphViewer.UpdateNodes();
            }
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                GraphViewer.ClearFloatingControls();
                GraphViewer.Graph.UpdateNodeValues();
                GraphViewer.UpdateNodes();
            }
        }

        private void assemblerButton_Click(object sender, EventArgs e)
        {
            var optionList = new List<ChooserControl>();
            var bestOption = new ItemChooserControl(null, "Best", "Best");
            optionList.Add(bestOption);

            var recipeNode = (RecipeNode)BaseNode;
            var recipe = recipeNode.BaseRecipe;

            var allowedAssemblers = DataCache.Assemblers.Values
                .Where(a => a.Enabled)
                .Where(a => a.Categories.Contains(recipe.Category))
                .Where(a => a.MaxIngredients >= recipe.Ingredients.Count);
            foreach (var assembler in allowedAssemblers.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Items.Values.SingleOrDefault(i => i.Name == assembler.Name);
                optionList.Add(new ItemChooserControl(item, assembler.FriendlyName, assembler.FriendlyName));
            }

            var chooserPanel = new ChooserPanel(optionList, GraphViewer);

            Point location = GraphViewer.ScreenToGraph(new Point(GraphViewer.Width / 2, GraphViewer.Height / 2));

            chooserPanel.Show(c => {
                if (c != null) {
                    if (c == bestOption) {
                        recipeNode.Assembler = null;
                    } else {
                        var assembler = DataCache.Assemblers.Single(a => a.Key == c.DisplayText).Value;
                        recipeNode.Assembler = assembler;
                    }
                    UpdateAssemblerButtons();
                    GraphViewer.Graph.UpdateNodeValues();
                    GraphViewer.UpdateNodes();
                }
            });
        }

        private void modulesButton_Click(object sender, EventArgs e)
        {
            var optionList = new List<ChooserControl>();
            var fastestOption = new ItemChooserControl(null, "Best", "Best");
            optionList.Add(fastestOption);

            var noneOption = new ItemChooserControl(null, "None", "None");
            optionList.Add(noneOption);

            var productivityOption = new ItemChooserControl(null, "Most Productive", "Most Productive");
            optionList.Add(productivityOption);

            var recipeNode = (RecipeNode)BaseNode;
            var recipe = recipeNode.BaseRecipe;

            var allowedModules = DataCache.Modules.Values
                .Where(a => a.Enabled)
                .Where(a => a.AllowedIn(recipe));

            foreach (var module in allowedModules.OrderBy(a => a.FriendlyName)) {
                var item = DataCache.Items.Values.SingleOrDefault(i => i.Name == module.Name);
                optionList.Add(new ItemChooserControl(item, module.FriendlyName, module.FriendlyName));
            }

            var chooserPanel = new ChooserPanel(optionList, GraphViewer);

            Point location = GraphViewer.ScreenToGraph(new Point(GraphViewer.Width / 2, GraphViewer.Height / 2));

            chooserPanel.Show(c => {
                if (c != null) {
                    if (c == fastestOption) {
                        recipeNode.NodeModules = ModuleSelector.Fastest;
                    } else if (c == noneOption) {
                        recipeNode.NodeModules = ModuleSelector.None;
                    } else if (c == productivityOption) {
                        recipeNode.NodeModules = ModuleSelector.Productive;
                    } else {
                        var module = DataCache.Modules.Single(a => a.Key == c.DisplayText).Value;
                        recipeNode.NodeModules = ModuleSelector.Specific(module);
                    }
                    UpdateAssemblerButtons();
                    GraphViewer.Graph.UpdateNodeValues();
                    GraphViewer.UpdateNodes();
                }
            });
        }

        private void productivityBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            double newAmount;
            var input = (TextBox)sender;
            if (double.TryParse(input.Text, out newAmount)) {
                BaseNode.ProductivityBonus = newAmount;
                GraphViewer.Graph.UpdateNodeValues();
                GraphViewer.UpdateNodes();
            }
        }

        private void speedBonusTextBox_TextChanged(object sender, EventArgs e)
        {
            double newAmount;
            var input = (TextBox)sender;
            if (double.TryParse(input.Text, out newAmount)) {
                BaseNode.SpeedBonus = newAmount;
                GraphViewer.Graph.UpdateNodeValues();
                GraphViewer.UpdateNodes();
            }
        }
    }
}
