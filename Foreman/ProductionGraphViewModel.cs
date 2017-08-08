namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using Controls;
    using Extensions;
    using Newtonsoft.Json.Linq;

    public class ProductionGraphViewModel
        : ViewModel, IInteractiveCanvasViewModel, ISerializable
    {
        private bool showAssemblers;
        private bool showMiners;
        private ProductionGraph graph;

        private double scale = 1.0;
        private Vector offset;
        private Rect viewbox = Rect.Empty;
        private Rect actualViewbox = Rect.Empty;
        private double actualWidth;
        private double actualHeight;

        public ProductionGraphViewModel(ProductionGraph graph)
        {
            Graph = graph;
        }

        public ProductionGraphViewModel(ProductionGraphViewModel source)
        {
            Graph = source.graph;
            foreach (var sourceElement in source.Elements)
                Elements.Add(sourceElement.Clone());
        }

        public ObservableCollection<GraphElement> Elements { get; } =
            new ObservableCollection<GraphElement>();

        public ObservableCollection<GraphElement> SelectedItems { get; } =
            new ObservableCollection<GraphElement>();

        public ProductionGraph Graph
        {
            get => graph;
            set
            {
                if (graph != null)
                    graph.NodeValuesUpdated -= OnGraphNodeValuesUpdated;
                graph = value;
                if (graph != null)
                    graph.NodeValuesUpdated += OnGraphNodeValuesUpdated;
            }
        }

        public double Scale
        {
            get => scale;
            set => SetProperty(ref scale, value);
        }

        public Vector Offset
        {
            get => offset;
            set => SetProperty(ref offset, value);
        }

        public Rect Viewbox
        {
            get => viewbox;
            set => SetProperty(ref viewbox, value);
        }

        public Rect ActualViewbox
        {
            get => actualViewbox;
            set => SetProperty(ref actualViewbox, value);
        }

        public double ActualWidth
        {
            get => actualWidth;
            set => SetProperty(ref actualWidth, value);
        }

        public double ActualHeight
        {
            get => actualHeight;
            set => SetProperty(ref actualHeight, value);
        }

        public bool ShowAssemblers
        {
            get => showAssemblers;
            set
            {
                if (SetProperty(ref showAssemblers, value))
                    UpdateNodes();
            }
        }

        public bool ShowMiners
        {
            get => showMiners;
            set
            {
                if (SetProperty(ref showMiners, value))
                    UpdateNodes();
            }
        }

        private void OnGraphNodeValuesUpdated(object sender, EventArgs e)
        {
            UpdateNodes();
        }

        public NodeElement GetElementForNode(ProductionNode node)
        {
            return Elements.OfType<NodeElement>().FirstOrDefault(e => e.DisplayedNode == node);
        }

        public void PositionNodes()
        {
            if (!Elements.Any())
                return;

            var nodeOrder = Graph.GetTopologicalSort();
            nodeOrder.Reverse();

            if (nodeOrder.Any()) {
                var nodePositions = new List<ProductionNode>[nodeOrder.Count];
                for (int i = 0; i < nodePositions.Length; ++i)
                    nodePositions[i] = new List<ProductionNode>();

                nodePositions.First().AddRange(nodeOrder.OfType<ConsumerNode>());
                foreach (RecipeNode node in nodeOrder.OfType<RecipeNode>()) {
                    bool positionFound = false;

                    for (int i = nodePositions.Length - 1; i >= 0 && !positionFound; --i) {
                        foreach (ProductionNode listNode in nodePositions[i]) {
                            if (listNode.CanUltimatelyTakeFrom(node)) {
                                nodePositions[i + 1].Add(node);
                                positionFound = true;
                                break;
                            }
                        }
                    }

                    if (!positionFound)
                        nodePositions.First().Add(node);
                }

                nodePositions.Last().AddRange(nodeOrder.OfType<SupplyNode>());

                int marginX = 100;
                int marginY = 200;
                double y = marginY;
                var tierWidths = new double[nodePositions.Length];
                for (int i = 0; i < nodePositions.Length; i++) {
                    var list = nodePositions[i];
                    double maxHeight = 0;
                    double x = marginX;

                    foreach (var node in list) {
                        NodeElement control = GetElementForNode(node);
                        control.Position = new Point(x, y);

                        var renderSize = control.RenderSize;

                        x += Math.Max(renderSize.Width, 150) + marginX;
                        maxHeight = Math.Max(Math.Max(renderSize.Height, 100), maxHeight);
                    }

                    if (maxHeight > 0) // Don't add any height for empty tiers
                        y += maxHeight + marginY;

                    tierWidths[i] = x;
                }

                double centerPoint = tierWidths.Last(i => i > marginX) / 2;
                for (int i = tierWidths.Length - 1; i >= 0; i--) {
                    double offset = centerPoint - tierWidths[i] / 2;

                    foreach (var node in nodePositions[i]) {
                        NodeElement element = GetElementForNode(node);
                        element.Position += new Vector(offset, 0);
                    }
                }
            }

            UpdateNodes();
        }

        public void UpdateNodes()
        {
            foreach (NodeElement node in Elements.OfType<NodeElement>().ToList())
                node.Update();
        }

        public void AddRemoveElements()
        {
            Elements.RemoveWhere(e => e is Connector c && !Graph.GetAllNodeLinks().Contains(c.DisplayedLink));
            Elements.RemoveWhere(e => e is NodeElement n && !Graph.Nodes.Contains(n.DisplayedNode));

            foreach (ProductionNode node in Graph.Nodes) {
                if (Elements.OfType<NodeElement>().All(e => e.DisplayedNode != node))
                    Elements.Add(new NodeElement(node, this));
            }

            foreach (NodeLink link in Graph.GetAllNodeLinks()) {
                if (Elements.OfType<Connector>().All(e => e.DisplayedLink != link)) {
                    var source = GetElementForNode(link.Supplier).Outputs.FirstOrDefault(x => x.Item == link.Item);
                    var destination = GetElementForNode(link.Consumer).Inputs.FirstOrDefault(x => x.Item == link.Item);
                    Elements.Add(new Connector(link, source, destination));
                }
            }

            UpdateNodes();
        }

        public async Task OnDataDropped(IDataObject data, Point screenPosition, Point position)
        {
            if (data.IsDataPresent<HashSet<Item>>()) {
                foreach (Item item in data.GetData<HashSet<Item>>()) {
                    NodeElement newElement = null;

                    var itemSupplyOption =
                        new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                    var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);
                    var itemPassthroughOption =
                        new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                    var optionList = new List<Choice>();
                    optionList.Add(itemPassthroughOption);
                    optionList.Add(itemOutputOption);
                    foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(r => r.Enabled)) {
                        if (recipe.Results.ContainsKey(item) && recipe.Category != "incinerator" &&
                            recipe.Category != "incineration") {
                            optionList.Add(new RecipeChoice(recipe,
                                string.Format("Create '{0}' recipe node", recipe.FriendlyName),
                                recipe.FriendlyName));
                        }
                    }
                    optionList.Add(itemSupplyOption);
                    foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(r => r.Enabled)) {
                        if (recipe.Ingredients.ContainsKey(item)) {
                            optionList.Add(new RecipeChoice(recipe,
                                string.Format("Create '{0}' recipe node", recipe.FriendlyName),
                                recipe.FriendlyName));
                        }
                    }

                    Choice c = await optionList.ChooseAsync(screenPosition);
                    if (c != null) {
                        if (c == itemSupplyOption) {
                            newElement = new NodeElement(SupplyNode.Create(item, Graph), this);
                        } else if (c == itemPassthroughOption) {
                            newElement = new NodeElement(PassthroughNode.Create(item, Graph), this);
                        } else if (c == itemOutputOption) {
                            newElement = new NodeElement(ConsumerNode.Create(item, Graph), this);
                        } else if (c is RecipeChoice rc) {
                            newElement = new NodeElement(RecipeNode.Create(rc.Recipe, Graph), this);
                        } else {
                            Trace.Fail("No handler for selected item");
                        }

                        newElement.Update();
                        newElement.Position = position;
                        Elements.Add(newElement);
                        Graph.UpdateNodeValues();
                    }
                }
            } else if (data.IsDataPresent<HashSet<Recipe>>()) {
                foreach (Recipe recipe in data.GetData<HashSet<Recipe>>()) {
                    var newElement = new NodeElement(RecipeNode.Create(recipe, Graph), this);
                    newElement.Update();
                    newElement.Position = position;
                    Elements.Add(newElement);
                    position += new Vector(75, 75);
                }
                Graph.UpdateNodeValues();
            }
        }

        void IInteractiveCanvasViewModel.Select(object item)
        {
            if (item is GraphElement e)
                SelectedItems.Add(e);
        }

        void IInteractiveCanvasViewModel.Unselect(object item)
        {
            if (item is GraphElement e)
                SelectedItems.Remove(e);
        }

        void IInteractiveCanvasViewModel.UnselectAll()
        {
            SelectedItems.Clear();
        }

        void IInteractiveCanvasViewModel.DeleteSelected()
        {
            var list = SelectedItems.ToList();
            SelectedItems.Clear();
            Delete(list);
        }

        public void Delete(IEnumerable<GraphElement> elements)
        {
            foreach (var element in elements) {
                switch (element) {
                    case NodeElement node:
                        node.DisplayedNode.Destroy();
                        Elements.Remove(node);
                        foreach (var connector in node.Pins.SelectMany(x => x.Connectors))
                            Elements.Remove(connector);
                        break;
                    case Connector connector:
                        connector.DisplayedLink.Destroy();
                        Elements.Remove(connector);
                        break;
                }
            }

            Graph.UpdateNodeValues();
        }

        public void Connect(Pin output, Pin input)
        {
            if (output.Kind == input.Kind)
                return;

            if (output.Kind == PinKind.Input) {
                var temp = output;
                output = input;
                input = temp;
            }

            var link = NodeLink.Create(output.Node.DisplayedNode, input.Node.DisplayedNode, input.Item);
            var connector = new Connector(link, output, input);
            Elements.Add(connector);
            Graph.UpdateNodeValues();
        }

        public async Task SuggestConnect(Pin pin, Point canvasPosition, Point screenPosition)
        {
            var startConnectionType = pin.Kind;
            NodeElement supplierElement = pin.Kind == PinKind.Output ? pin.Node : null;
            NodeElement consumerElement = pin.Kind == PinKind.Input ? pin.Node : null;
            Item item = pin.Item;

            if (startConnectionType == PinKind.Output && consumerElement == null) {
                var recipeOptionList = new List<Choice>();

                var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);
                var itemPassthroughOption = new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                recipeOptionList.Add(itemOutputOption);
                recipeOptionList.Add(itemPassthroughOption);

                foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(
                    r => r.Ingredients.Keys.Contains(item) && r.Enabled && r.Category != "incinerator" &&
                         r.Category != "incineration")) {
                    recipeOptionList.Add(new RecipeChoice(recipe, "Use recipe " + recipe.FriendlyName,
                        recipe.FriendlyName));
                }

                var c = await recipeOptionList.ChooseAsync(screenPosition);
                if (c != null) {
                    NodeElement newElement = null;
                    if (c is RecipeChoice rc) {
                        Recipe selectedRecipe = rc.Recipe;
                        newElement = new NodeElement(RecipeNode.Create(selectedRecipe, Graph), this);
                    } else if (c == itemOutputOption) {
                        Item selectedItem = ((ItemChoice)c).Item;
                        newElement = new NodeElement(ConsumerNode.Create(selectedItem, Graph), this);
                        ((ConsumerNode)newElement.DisplayedNode).RateType = RateType.Auto;
                    } else if (c == itemPassthroughOption) {
                        Item selectedItem = ((ItemChoice)c).Item;
                        newElement = new NodeElement(PassthroughNode.Create(selectedItem, Graph), this);
                        ((PassthroughNode)newElement.DisplayedNode).RateType = RateType.Auto;
                    } else {
                        Trace.Fail("Unhandled option: " + c.ToString());
                    }

                    newElement.Update();
                    newElement.Position = canvasPosition;
                    //new Vector(-newElement.Width / 2, -newElement.Height / 2));

                    var link = NodeLink.Create(supplierElement.DisplayedNode, newElement.DisplayedNode, item);
                    var source = supplierElement.Outputs.First(x => x.Item == item);
                    var destination = newElement.Inputs.First(x => x.Item == item);
                    var connector = new Connector(link, source, destination);
                    Elements.Add(newElement);
                    Elements.Add(connector);
                }

                Graph.UpdateNodeValues();
            } else if (startConnectionType == PinKind.Input && supplierElement == null) {
                var recipeOptionList = new List<Choice>();

                var itemSupplyOption = new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                var itemPassthroughOption = new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                recipeOptionList.Add(itemSupplyOption);
                recipeOptionList.Add(itemPassthroughOption);

                foreach (Recipe recipe in DataCache.Current.Recipes.Values.Where(
                    r => r.Results.Keys.Contains(item) && r.Enabled && r.Category != "incinerator" &&
                         r.Category != "incineration")) {
                    if (recipe.Category != "incinerator" && recipe.Category != "incineration") {
                        recipeOptionList.Add(new RecipeChoice(recipe, "Use recipe " + recipe.FriendlyName,
                            recipe.FriendlyName));
                    }
                }

                var c = await recipeOptionList.ChooseAsync(screenPosition);
                if (c != null) {
                    NodeElement newElement = null;
                    if (c is RecipeChoice rc) {
                        Recipe selectedRecipe = rc.Recipe;
                        newElement = new NodeElement(RecipeNode.Create(selectedRecipe, Graph), this);
                    } else if (c == itemSupplyOption) {
                        Item selectedItem = ((ItemChoice)c).Item;
                        newElement = new NodeElement(SupplyNode.Create(selectedItem, Graph), this);
                    } else if (c == itemPassthroughOption) {
                        Item selectedItem = ((ItemChoice)c).Item;
                        newElement = new NodeElement(PassthroughNode.Create(selectedItem, Graph), this);
                        ((PassthroughNode)newElement.DisplayedNode).RateType = RateType.Auto;
                    } else {
                        Trace.Fail("Unhandled option: " + c.ToString());
                    }
                    newElement.Update();
                    newElement.Position = canvasPosition;
                    //new Vector(-newElement.Width / 2, -newElement.Height / 2));

                    var link = NodeLink.Create(newElement.DisplayedNode, consumerElement.DisplayedNode, item);
                    var source = newElement.Outputs.First(x => x.Item == item);
                    var destination = consumerElement.Inputs.First(x => x.Item == item);
                    var connector = new Connector(link, source, destination);
                    Elements.Add(newElement);
                    Elements.Add(connector);
                }

                Graph.UpdateNodeValues();
            }
        }

        public ProductionGraphViewModel(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AmountType", Graph.SelectedAmountType);
            info.AddValue("Unit", Graph.SelectedUnit);
            info.AddValue("Nodes", Graph.Nodes);
            info.AddValue("NodeLinks", Graph.GetAllNodeLinks());
            info.AddValue("ElementLocations",
                Graph.Nodes.Select(x => GetElementForNode(x).Position).ToList());
            info.AddValue("EnabledAssemblers",
                DataCache.Current.Assemblers.Values.Where(a => a.Enabled).Select(a => a.Name));
            info.AddValue("EnabledMiners",
                DataCache.Current.Miners.Values.Where(m => m.Enabled).Select(m => m.Name));
            info.AddValue("EnabledModules",
                DataCache.Current.Modules.Values.Where(m => m.Enabled).Select(m => m.Name));
            info.AddValue("EnabledMods", DataCache.Current.Mods.Where(m => m.Enabled).Select(m => m.Name));
            info.AddValue("EnabledRecipes",
                DataCache.Current.Recipes.Values.Where(r => r.Enabled).Select(r => r.Name));
        }

        public async Task LoadFromJson(JObject json)
        {
            var notifications = new List<string>();

            Graph.Nodes.Clear();
            Elements.Clear();

            //Has to go first, as all other data depends on which mods are loaded
            var enabledMods = json["EnabledMods"].ToSet(t => (string)t);
            foreach (Mod mod in DataCache.Current.Mods) {
                mod.Enabled = enabledMods.Contains(mod.Name);
            }

            var mods = DataCache.Current.Mods.Where(m => m.Enabled).Select(m => m.Name).ToList();
            await Task.Run(() => DataCache.Reload(mods));

            Graph.SelectedAmountType = (AmountType)(int)json["AmountType"];
            Graph.SelectedUnit = (RateUnit)(int)json["Unit"];

            List<JToken> nodes = json["Nodes"].ToList();
            foreach (var node in nodes) {
                ProductionNode newNode = null;

                switch ((string)node["NodeType"]) {
                    case "Consumer": {
                            string itemName = (string)node["ItemName"];
                            if (DataCache.Current.Items.ContainsKey(itemName)) {
                                Item item = DataCache.Current.Items[itemName];
                                newNode = ConsumerNode.Create(item, Graph);
                            } else {
                                Item missingItem = new Item(itemName);
                                missingItem.IsMissingItem = true;
                                newNode = ConsumerNode.Create(missingItem, Graph);
                            }
                            break;
                        }
                    case "Supply": {
                            string itemName = (string)node["ItemName"];
                            if (DataCache.Current.Items.ContainsKey(itemName)) {
                                Item item = DataCache.Current.Items[itemName];
                                newNode = SupplyNode.Create(item, Graph);
                            } else {
                                Item missingItem = new Item(itemName);
                                missingItem.IsMissingItem = true;
                                DataCache.Current.Items.Add(itemName, missingItem);
                                newNode = SupplyNode.Create(missingItem, Graph);
                            }
                            break;
                        }
                    case "PassThrough": {
                            string itemName = (string)node["ItemName"];
                            if (DataCache.Current.Items.ContainsKey(itemName)) {
                                Item item = DataCache.Current.Items[itemName];
                                newNode = PassthroughNode.Create(item, Graph);
                            } else {
                                Item missingItem = new Item(itemName);
                                missingItem.IsMissingItem = true;
                                DataCache.Current.Items.Add(itemName, missingItem);
                                newNode = PassthroughNode.Create(missingItem, Graph);
                            }
                            break;
                        }
                    case "Recipe": {
                            string recipeName = (string)node["RecipeName"];
                            if (DataCache.Current.Recipes.ContainsKey(recipeName)) {
                                Recipe recipe = DataCache.Current.Recipes[recipeName];
                                newNode = RecipeNode.Create(recipe, Graph);
                            } else {
                                Recipe missingRecipe = new Recipe(recipeName, 0f, new Dictionary<Item, float>(),
                                    new Dictionary<Item, float>());
                                missingRecipe.IsMissingRecipe = true;
                                DataCache.Current.Recipes.Add(recipeName, missingRecipe);
                                newNode = RecipeNode.Create(missingRecipe, Graph);
                            }

                            if (node["Assembler"] != null) {
                                var assemblerKey = (string)node["Assembler"];
                                if (DataCache.Current.Assemblers.ContainsKey(assemblerKey)) {
                                    ((RecipeNode)newNode).Assembler = DataCache.Current.Assemblers[assemblerKey];
                                }
                            }

                        ((RecipeNode)newNode).Modules = ModuleSelector.Load(node);
                            break;
                        }
                    default: {
                            Trace.Fail("Unknown node type: " + node["NodeType"]);
                            break;
                        }
                }

                if (newNode != null) {
                    newNode.RateType = (RateType)(int)node["RateType"];
                    if (newNode.RateType == RateType.Manual) {
                        if (node["DesiredRate"] != null) {
                            newNode.DesiredRate = (float)node["DesiredRate"];
                        } else {
                            // Legacy data format stored desired rate in actual
                            newNode.DesiredRate = (float)node["ActualRate"];
                        }
                    }
                    if (node["BeaconModules"] != null) {
                        foreach (var entry in node["BeaconModules"].ToObject<Dictionary<string, int>>()) {
                            var module = DataCache.Current.Modules.GetValueOrDefault(entry.Key);
                            if (module != null)
                                newNode.BeaconModules.Add(module, entry.Value);
                        }
                    }

                    if (node["SpeedBonus"] != null)
                        newNode.BeaconModules.OverrideSpeedBonus = node["SpeedBonus"].Value<double?>();
                    if (node["ProductivityBonus"] != null)
                        newNode.BeaconModules.OverrideProductivityBonus = node["ProductivityBonus"].Value<double?>();
                    if (node["ConsumptionBonus"] != null)
                        newNode.BeaconModules.OverrideConsumptionBonus = node["ConsumptionBonus"].Value<double?>();
                }
            }

            List<JToken> nodeLinks = json["NodeLinks"].ToList();
            foreach (var nodeLink in nodeLinks) {
                ProductionNode supplier = Graph.Nodes[(int)nodeLink["Supplier"]];
                ProductionNode consumer = Graph.Nodes[(int)nodeLink["Consumer"]];

                string itemName = (string)nodeLink["Item"];
                if (!DataCache.Current.Items.ContainsKey(itemName)) {
                    Item missingItem = new Item(itemName);
                    missingItem.IsMissingItem = true;
                    DataCache.Current.Items.Add(itemName, missingItem);
                }
                Item item = DataCache.Current.Items[itemName];

                if (!NodeLink.CanLink(supplier, consumer, item)) {
                    notifications.Add($"Unable to link '{supplier}' to '{consumer}' for item '{item}'.");
                    continue;
                }

                NodeLink.Create(supplier, consumer, item);
            }

            var enabledAssemblers = json["EnabledAssemblers"].ToSet(t => (string)t);
            foreach (Assembler assembler in DataCache.Current.Assemblers.Values)
                assembler.Enabled = enabledAssemblers.Contains(assembler.Name);

            var enabledMiners = json["EnabledMiners"].ToSet(t => (string)t);
            foreach (Miner miner in DataCache.Current.Miners.Values)
                miner.Enabled = enabledMiners.Contains(miner.Name);

            var enabledModules = json["EnabledModules"].ToSet(t => (string)t);
            foreach (Module module in DataCache.Current.Modules.Values)
                module.Enabled = enabledModules.Contains(module.Name);

            if (json.TryGetValue("EnabledRecipes", out JToken enabledRecipesToken)) {
                var enabledRecipes = enabledRecipesToken.ToSet(t => (string)t);
                foreach (Recipe recipe in DataCache.Current.Recipes.Values)
                    recipe.Enabled = enabledRecipes.Contains(recipe.Name);
            }

            Graph.UpdateNodeValues();
            AddRemoveElements();

            var elementLocations = json["ElementLocations"].ToList();
            for (int i = 0; i < elementLocations.Count; ++i) {
                var element = GetElementForNode(Graph.Nodes[i]);
                element.Position = elementLocations[i].ToObject<Point>();
            }

            LimitViewToBounds();

            if (notifications.Count > 0) {
                var message =
                    "Failed to completely load the graph. " +
                    "This may be due to different game data.\n\n" +
                    string.Join("\n", notifications);
                MessageBox.Show(
                    message, "Invalid Graph",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Rect ComputeGraphBounds(Thickness? margin = null)
        {
            if (margin == null)
                margin = new Thickness(80);

            var pos = new Point(double.MaxValue, double.MaxValue);
            foreach (NodeElement element in Elements.OfType<NodeElement>())
                pos = PointUtils.Min(element.Position, pos);

            if (pos.X == double.MaxValue)
                return Rect.Empty;

            double width = 0;
            double height = 0;
            foreach (NodeElement element in Elements.OfType<NodeElement>()) {
                width = Math.Max(element.Position.X + element.RenderWidth - pos.X, width);
                height = Math.Max(element.Position.Y + element.RenderHeight - pos.Y, height);
            }

            return new Rect(
                pos.X - margin.Value.Left,
                pos.Y - margin.Value.Top,
                width + margin.Value.Left + margin.Value.Right,
                height + margin.Value.Top + margin.Value.Bottom);
        }

        private void LimitViewToBounds()
        {
            if (actualWidth == 0 || actualHeight == 0)
                return;

            var bounds = ComputeGraphBounds();

            var newScale = Math.Min(
                actualWidth / bounds.Width,
                actualHeight / bounds.Height);

            var newOffset = new Vector(
                (bounds.X + bounds.Width / 2) * newScale - actualWidth / 2,
                (bounds.Y + bounds.Height / 2) * newScale - actualHeight / 2);

            Scale = newScale;
            Offset = newOffset;
        }
    }
}
