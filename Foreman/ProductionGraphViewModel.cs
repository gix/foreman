namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using Controls;
    using Extensions;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public class ProductionGraphViewModel
        : ViewModel, IInteractiveCanvasViewModel, ISerializable
    {
        private static readonly ProductionGraph emptyGraph = new();

        private bool showAssemblers;
        private bool showMiners;
        private ProductionGraph? graph;

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

        public ObservableCollection<GraphElement> Elements { get; } = new();

        public ObservableCollection<GraphElement> SelectedItems { get; } = new();

        [AllowNull]
        public ProductionGraph Graph
        {
            get => graph ?? emptyGraph;
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

        private void OnGraphNodeValuesUpdated(object? sender, ISet<ProductionNode>? nodes)
        {
            UpdateNodes(nodes);
        }

        public NodeElement GetElementForNode(ProductionNode node)
        {
            return Elements.OfType<NodeElement>().First(e => e.DisplayedNode == node);
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

        public void UpdateNodes(ISet<ProductionNode>? nodes = null)
        {
            var nodeElements = Elements.OfType<NodeElement>();
            if (nodes != null)
                nodeElements = nodeElements.Where(x => nodes.Contains(x.DisplayedNode));

            foreach (NodeElement node in nodeElements.ToList())
                node.Update();
        }

        public void AddRemoveElements()
        {
            var graphNodes = Graph.Nodes.ToSet();
            var graphLinks = Graph.GetAllNodeLinks().ToSet();

            var viewLinks = Elements.OfType<Connector>().Select(x => x.DisplayedLink).ToSet();
            var nodeMap = Elements.OfType<NodeElement>().ToDictionary(x => x.DisplayedNode, x => x);

            Elements.RemoveWhere(e => e is Connector c && !graphLinks.Contains(c.DisplayedLink));
            Elements.RemoveWhere(e => e is NodeElement n && !graphNodes.Contains(n.DisplayedNode));

            foreach (ProductionNode node in graphNodes) {
                if (!nodeMap.ContainsKey(node)) {
                    var element = new NodeElement(node, this);
                    nodeMap.Add(node, element);
                    Elements.Add(element);
                }
            }

            foreach (NodeLink link in graphLinks) {
                if (!viewLinks.Contains(link)) {
                    var source = nodeMap[link.Supplier].GetOutputFor(link.Item);
                    var destination = nodeMap[link.Consumer].GetInputFor(link.Item);
                    Elements.Add(new Connector(link, source, destination));
                }
            }

            UpdateNodes();
        }

        private static bool IsEligible(Recipe recipe)
        {
            return recipe.Category != "incinerator" && recipe.Category != "incineration";
        }

        public async Task OnDataDropped(IDataObject data, Point screenPosition, Point position)
        {
            if (data.IsDataPresent<HashSet<Item>>()) {
                foreach (Item item in data.GetData<HashSet<Item>>()) {
                    var itemSupplyOption =
                        new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                    var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);
                    var itemPassthroughOption =
                        new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                    var optionList = new List<Choice>();
                    optionList.Add(itemPassthroughOption);
                    optionList.Add(itemOutputOption);
                    foreach (Recipe recipe in DataCache.Current.RecipesSupplying(item).Where(IsEligible)) {
                        optionList.Add(new RecipeChoice(recipe,
                            $"Create '{recipe.FriendlyName}' recipe node",
                            recipe.FriendlyName));
                    }
                    optionList.Add(itemSupplyOption);
                    foreach (Recipe recipe in DataCache.Current.RecipesConsuming(item)) {
                        optionList.Add(new RecipeChoice(recipe,
                            $"Create '{recipe.FriendlyName}' recipe node",
                            recipe.FriendlyName));
                    }

                    Choice? c = await optionList.ChooseAsync(screenPosition);
                    if (c != null) {
                        NodeElement newElement;
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
                            return;
                        }

                        newElement.Update();
                        newElement.Position = position;
                        Elements.Add(newElement);
                        Graph.UpdateNodeValues();
                        position += new Vector(75, 75);
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

        private Guid clipboardValidityToken;
        private HashSet<GraphElement>? clipboardContent;

        void IInteractiveCanvasViewModel.CopySelected()
        {
            var items = new HashSet<GraphElement>(SelectedItems);

            foreach (var connector in SelectedItems.OfType<NodeElement>().SelectMany(x => x.Outputs).SelectMany(x => x.Connectors)) {
                if (connector.Source == null ||
                    connector.Destination == null)
                    continue;

                if (!items.Contains(connector.Source.Node) ||
                    !items.Contains(connector.Destination.Node))
                    continue;

                items.Add(connector);
            }

            clipboardValidityToken = Guid.NewGuid();
            clipboardContent = items;
            Clipboard.SetData("Foreman.ProductionGraph.Elements", clipboardValidityToken);
        }

        List<object>? IInteractiveCanvasViewModel.Paste()
        {
            var token = Clipboard.GetData("Foreman.ProductionGraph.Elements") as Guid?;
            var sourceElements = clipboardContent;
            if (token != clipboardValidityToken ||
                sourceElements == null || sourceElements.Count == 0)
                return null;

            var inputSet = sourceElements;
            var elements = new List<GraphElement>();
            var map = new Dictionary<NodeElement, NodeElement>();
            var elementOffset = new Vector(20, 20);

            foreach (var element in inputSet.OfType<NodeElement>()) {
                var clonedNode = CloneNode(element.DisplayedNode);
                var clonedElement = new NodeElement(clonedNode, element.Parent);
                clonedElement.Position = element.Position + elementOffset;

                elements.Add(clonedElement);
                map.Add(element, clonedElement);
            }

            foreach (var sourceConnector in inputSet.OfType<Connector>()) {
                if (sourceConnector.Source == null ||
                    sourceConnector.Destination == null)
                    continue;

                // Skip connections to nodes not copied.
                if (!inputSet.Contains(sourceConnector.Source.Node) ||
                    !inputSet.Contains(sourceConnector.Destination.Node))
                    continue;

                NodeElement srcElement = map[sourceConnector.Source.Node];
                NodeElement dstElement = map[sourceConnector.Destination.Node];

                Item item = sourceConnector.Source.Item;

                Pin? sourcePin = srcElement.GetOutputFor(item);
                Pin? destinationPin = dstElement.GetInputFor(item);
                if (sourcePin == null || destinationPin == null)
                    continue;

                var link = NodeLink.Create(sourcePin.Node.DisplayedNode, destinationPin.Node.DisplayedNode, item);
                if (link == null)
                    continue;

                var connector = new Connector(link, sourcePin, destinationPin);
                elements.Add(connector);
            }

            Elements.AddRange(elements);

            Graph.UpdateNodeValues();

            return elements.OfType<NodeElement>().ToList<object>();
        }

        private static ProductionNode CloneNode(ProductionNode node)
        {
            return node switch {
                RecipeNode r => r.Clone(r.Graph),
                SupplyNode s => s.Clone(s.Graph),
                ConsumerNode c => c.Clone(c.Graph),
                PassthroughNode p => p.Clone(p.Graph),
                _ => throw new ArgumentOutOfRangeException(nameof(node), node, null)
            };
        }

        public void Delete(IEnumerable<GraphElement> elements)
        {
            foreach (var element in elements) {
                switch (element) {
                    case NodeElement node:
                        Delete(node);
                        break;
                    case Connector connector:
                        Delete(connector);
                        break;
                }
            }

            Graph.UpdateNodeValues();
        }

        private void Delete(NodeElement node)
        {
            if (Elements.Remove(node)) {
                node.DisplayedNode.Destroy();
                foreach (var connector in node.Connectors.ToList())
                    Delete(connector);
            }
        }

        private void Delete(Connector connector)
        {
            if (Elements.Remove(connector)) {
                connector.Source = null;
                connector.Destination = null;
                connector.DisplayedLink.Destroy();
            }
        }

        public void Connect(Pin output, Pin input)
        {
            if (output.Kind == input.Kind)
                return;

            if (output.Kind == PinKind.Input) {
                (output, input) = (input, output);
            }

            var link = NodeLink.Create(output.Node.DisplayedNode, input.Node.DisplayedNode, input.Item);
            if (link == null)
                return;

            var connector = new Connector(link, output, input);
            Elements.Add(connector);
            Graph.UpdateNodeValues();
        }

        public async Task SuggestConnect(Pin pin, Point canvasPosition, Point screenPosition)
        {
            var startConnectionType = pin.Kind;
            Item item = pin.Item;

            if (startConnectionType == PinKind.Output) {
                NodeElement supplierElement = pin.Node;

                var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);
                var itemPassthroughOption = new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                var recipeOptionList = new List<Choice> {
                    itemOutputOption,
                    itemPassthroughOption
                };
                AddChoices(recipeOptionList, DataCache.Current.RecipesConsuming(item).Where(IsEligible));

                var c = await recipeOptionList.ChooseAsync(screenPosition);
                if (c != null) {
                    NodeElement newElement;
                    if (c is RecipeChoice rc) {
                        newElement = new NodeElement(RecipeNode.Create(rc.Recipe, Graph), this);
                    } else if (c == itemOutputOption) {
                        var node = ConsumerNode.Create(((ItemChoice)c).Item!, Graph);
                        node.RateType = RateType.Auto;
                        newElement = new NodeElement(node, this);
                    } else if (c == itemPassthroughOption) {
                        var node = PassthroughNode.Create(((ItemChoice)c).Item!, Graph);
                        node.RateType = RateType.Auto;
                        newElement = new NodeElement(node, this);
                    } else {
                        Trace.Fail("Unhandled option: " + c);
                        return;
                    }

                    var link = NodeLink.Create(supplierElement.DisplayedNode, newElement.DisplayedNode, item);
                    if (link == null) {
                        Debug.Fail("NodeLink null");
                        return;
                    }

                    newElement.Update();
                    newElement.Position = canvasPosition;

                    var source = supplierElement.Outputs.First(x => x.Item == item);
                    var destination = newElement.Inputs.First(x => x.Item == item);
                    var connector = new Connector(link, source, destination);
                    Elements.Add(newElement);
                    Elements.Add(connector);
                }

                Graph.UpdateNodeValues();
            } else if (startConnectionType == PinKind.Input) {
                NodeElement consumerElement = pin.Node;

                var itemSupplyOption = new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                var itemPassthroughOption = new ItemChoice(item, "Create pass-through node", item.FriendlyName);

                var recipeOptionList = new List<Choice> {
                    itemSupplyOption,
                    itemPassthroughOption
                };
                AddChoices(recipeOptionList, DataCache.Current.RecipesSupplying(item).Where(IsEligible));

                var c = await recipeOptionList.ChooseAsync(screenPosition);
                if (c != null) {
                    NodeElement newElement;
                    if (c is RecipeChoice rc) {
                        newElement = new NodeElement(RecipeNode.Create(rc.Recipe, Graph), this);
                    } else if (c == itemSupplyOption) {
                        newElement = new NodeElement(SupplyNode.Create(((ItemChoice)c).Item!, Graph), this);
                    } else if (c == itemPassthroughOption) {
                        var node = PassthroughNode.Create(((ItemChoice)c).Item!, Graph);
                        node.RateType = RateType.Auto;
                        newElement = new NodeElement(node, this);
                    } else {
                        Trace.Fail("Unhandled option: " + c);
                        return;
                    }

                    var link = NodeLink.Create(newElement.DisplayedNode, consumerElement.DisplayedNode, item);
                    if (link == null) {
                        Debug.Fail("NodeLink null");
                        return;
                    }

                    newElement.Update();
                    newElement.Position = canvasPosition;

                    var source = newElement.Outputs.First(x => x.Item == item);
                    var destination = consumerElement.Inputs.First(x => x.Item == item);
                    var connector = new Connector(link, source, destination);
                    Elements.Add(newElement);
                    Elements.Add(connector);
                }

                Graph.UpdateNodeValues();
            }
        }

        private static void AddChoices(List<Choice> choices, IEnumerable<Recipe> recipes)
        {
            choices.AddRange(
                recipes.Select(x =>
                    new RecipeChoice(x, "Use recipe " + x.FriendlyName, x.FriendlyName)));
        }

        public ProductionGraphViewModel(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AmountType", Graph.SelectedAmountType);
            info.AddValue("Unit", Graph.SelectedUnit);
            info.AddValue("ModuleStrategy", Graph.SelectedModuleStrategy);
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

        public async Task LoadFromJson(JObject json, Func<Task> reloadData)
        {
            var notifications = new List<string>();

            Graph.Nodes.Clear();
            Elements.Clear();

            //Has to go first, as all other data depends on which mods are loaded
            var enabledMods = json["EnabledMods"]!.ToSet(t => (string)t!);
            bool modified = false;
            foreach (Mod mod in DataCache.Current.Mods) {
                var modEnabled = enabledMods.Contains(mod.Name);
                if (modEnabled != mod.Enabled)
                    modified = true;
                mod.Enabled = modEnabled;
            }

            if (modified) {
                await reloadData();
            }

            Graph.SelectedAmountType = (AmountType)(int)json["AmountType"]!;
            Graph.SelectedUnit = (RateUnit)(int)json["Unit"]!;
            if (json["ModuleStrategy"] is { } t)
                Graph.SelectedModuleStrategy = ModuleSelector.Load(t);

            List<JToken> nodes = json["Nodes"]!.ToList();
            foreach (var node in nodes) {
                ProductionNode? newNode = null;

                switch ((string)node["NodeType"]!) {
                    case "Consumer": {
                        string itemName = (string)node["ItemName"]!;
                        if (DataCache.Current.Items.ContainsKey(itemName)) {
                            Item item = DataCache.Current.Items[itemName];
                            newNode = ConsumerNode.Create(item, Graph);
                        } else {
                            var missingItem = new Item(itemName);
                            missingItem.IsMissingItem = true;
                            newNode = ConsumerNode.Create(missingItem, Graph);
                        }
                        break;
                    }
                    case "Supply": {
                        string itemName = (string)node["ItemName"]!;
                        if (DataCache.Current.Items.ContainsKey(itemName)) {
                            Item item = DataCache.Current.Items[itemName];
                            newNode = SupplyNode.Create(item, Graph);
                        } else {
                            var missingItem = new Item(itemName);
                            missingItem.IsMissingItem = true;
                            DataCache.Current.Items.Add(itemName, missingItem);
                            newNode = SupplyNode.Create(missingItem, Graph);
                        }
                        break;
                    }
                    case "PassThrough": {
                        string itemName = (string)node["ItemName"]!;
                        if (DataCache.Current.Items.ContainsKey(itemName)) {
                            Item item = DataCache.Current.Items[itemName];
                            newNode = PassthroughNode.Create(item, Graph);
                        } else {
                            var missingItem = new Item(itemName);
                            missingItem.IsMissingItem = true;
                            DataCache.Current.Items.Add(itemName, missingItem);
                            newNode = PassthroughNode.Create(missingItem, Graph);
                        }
                        break;
                    }
                    case "Recipe": {
                        string recipeName = (string)node["RecipeName"]!;
                        if (DataCache.Current.Recipes.ContainsKey(recipeName)) {
                            Recipe recipe = DataCache.Current.Recipes[recipeName];
                            newNode = RecipeNode.Create(recipe, Graph);
                        } else {
                            var missingRecipe = new Recipe(recipeName, 0f, new Dictionary<Item, float>(),
                                new Dictionary<Item, float>());
                            missingRecipe.IsMissingRecipe = true;
                            DataCache.Current.Recipes.Add(recipeName, missingRecipe);
                            newNode = RecipeNode.Create(missingRecipe, Graph);
                        }

                        if (node["Assembler"] != null) {
                            var assemblerKey = (string)node["Assembler"]!;
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
                    newNode.RateType = (RateType)(int)node["RateType"]!;
                    if (newNode.RateType == RateType.Manual) {
                        if (node["DesiredRate"] != null) {
                            newNode.DesiredRate = (float)node["DesiredRate"]!;
                        } else {
                            // Legacy data format stored desired rate in actual
                            newNode.DesiredRate = (float)node["ActualRate"]!;
                        }
                    }
                    if (node["BeaconModules"] != null) {
                        foreach (var entry in node["BeaconModules"]!.ToObject<Dictionary<string, int>>()!) {
                            var module = DataCache.Current.Modules.GetValueOrDefault(entry.Key);
                            if (module != null)
                                newNode.BeaconModules.Add(module, entry.Value);
                        }
                    }

                    if (node["SpeedBonus"] != null)
                        newNode.BeaconModules.OverrideSpeedBonus = node["SpeedBonus"]?.Value<double?>();
                    if (node["ProductivityBonus"] != null)
                        newNode.BeaconModules.OverrideProductivityBonus = node["ProductivityBonus"]?.Value<double?>();
                    if (node["ConsumptionBonus"] != null)
                        newNode.BeaconModules.OverrideConsumptionBonus = node["ConsumptionBonus"]?.Value<double?>();
                }
            }

            List<JToken> nodeLinks = json["NodeLinks"]!.ToList();
            foreach (var nodeLink in nodeLinks) {
                ProductionNode supplier = Graph.Nodes[(int)nodeLink["Supplier"]!];
                ProductionNode consumer = Graph.Nodes[(int)nodeLink["Consumer"]!];

                string itemName = (string)nodeLink["Item"]!;
                if (!DataCache.Current.Items.ContainsKey(itemName)) {
                    var missingItem = new Item(itemName);
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

            var enabledAssemblers = json["EnabledAssemblers"]!.ToSet(t => (string)t!);
            foreach (Assembler assembler in DataCache.Current.Assemblers.Values)
                assembler.Enabled = enabledAssemblers.Contains(assembler.Name);

            var enabledMiners = json["EnabledMiners"]!.ToSet(t => (string)t!);
            foreach (Miner miner in DataCache.Current.Miners.Values)
                miner.Enabled = enabledMiners.Contains(miner.Name);

            var enabledModules = json["EnabledModules"]!.ToSet(t => (string)t!);
            foreach (Module module in DataCache.Current.Modules.Values)
                module.Enabled = enabledModules.Contains(module.Name);

            if (json.TryGetValue("EnabledRecipes", out JToken? enabledRecipesToken)) {
                var enabledRecipes = enabledRecipesToken.ToSet(t => (string)t!);
                foreach (Recipe recipe in DataCache.Current.Recipes.Values)
                    recipe.Enabled = enabledRecipes.Contains(recipe.Name);
            }

            Graph.UpdateNodeValues();
            AddRemoveElements();

            var elementLocations = json["ElementLocations"]!.ToList();
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
            margin ??= new Thickness(80);

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
