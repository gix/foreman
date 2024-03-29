namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum AmountType
    {
        FixedAmount,
        Rate
    }

    public enum RateUnit
    {
        PerMinute,
        PerSecond
    }

    public class ProductionGraph
    {
        private int[,]? adjacencyMatrixCache;
        private ModuleSelector selectedModuleStrategy = ModuleSelector.Fastest;

        public List<ProductionNode> Nodes { get; } = new();

        public RateUnit SelectedUnit { get; set; } = RateUnit.PerSecond;

        public AmountType SelectedAmountType { get; set; } = AmountType.FixedAmount;

        public ModuleSelector SelectedModuleStrategy
        {
            get => selectedModuleStrategy;
            set
            {
                if (selectedModuleStrategy == value)
                    return;
                selectedModuleStrategy = value;
                UpdateModuleStrategy();
            }
        }

        public IEnumerable<NodeLink> GetAllNodeLinks()
        {
            return Nodes.SelectMany(node => node.InputLinks);
        }

        public void InvalidateCaches()
        {
            adjacencyMatrixCache = null;
        }

        public int[,] AdjacencyMatrix
        {
            get
            {
                if (adjacencyMatrixCache == null) {
                    int[,] matrix = new int[Nodes.Count, Nodes.Count];
                    for (int i = 0; i < Nodes.Count; i++) {
                        for (int j = 0; j < Nodes.Count; j++) {
                            if (Nodes[j].InputLinks.Any(l => l.Supplier == Nodes[i])) {
                                matrix[i, j] = 1;
                            }
                        }
                    }
                    adjacencyMatrixCache = matrix;
                }
                return (int[,])adjacencyMatrixCache.Clone();
            }
        }

        public IEnumerable<ProductionNode> GetSuppliers(Item item)
        {
            return Nodes.Where(x => x.Outputs.Contains(item));
        }

        public IEnumerable<ProductionNode> GetConsumers(Item item)
        {
            return Nodes.Where(x => x.Inputs.Contains(item));
        }

        public void LinkUpAllInputs()
        {
            var nodesToVisit = new HashSet<ProductionNode>(Nodes);

            while (nodesToVisit.Any()) {
                ProductionNode currentNode = nodesToVisit.First();
                nodesToVisit.Remove(nodesToVisit.First());

                nodesToVisit.UnionWith(CreateOrLinkAllPossibleRecipeNodes(currentNode));
                nodesToVisit.UnionWith(CreateOrLinkAllPossibleSupplyNodes(currentNode));
                CreateAllPossibleInputLinks();
            }
        }

        public void LinkUpAllOutputs()
        {
            foreach (ProductionNode node in Nodes.ToList()) {
                foreach (Item item in node.Outputs) {
                    // TODO: Write unit tests, figure out what to do about the GetExcessOutput check here
                    // if (node.GetExcessOutput(item) > 0 || !node.OutputLinks.Any(l => l.Item == item))
                    if (node.OutputLinks.All(l => l.Item != item)) {
                        if (Nodes.Any(n => n.Inputs.Contains(item) && (n.RateType == RateType.Auto) &&
                                           !(n.InputLinks.Any(l => l.Supplier == node)))) {
                            NodeLink.Create(node, Nodes.First(n => n.Inputs.Contains(item)), item);
                        } else {
                            var newNode = ConsumerNode.Create(item, this);
                            newNode.RateType = RateType.Auto;
                            NodeLink.Create(node, newNode, item);
                        }
                    }
                }
            }
        }

        public void UpdateLinkThroughputs()
        {
            /*
            foreach (NodeLink link in GetAllNodeLinks())
            {
                link.Throughput = 0;
            }

            foreach (ProductionNode node in Nodes)
            {
                foreach (Item item in node.Outputs)
                {
                    foreach (NodeLink link in node.OutputLinks)
                    {
                        link.Throughput += Math.Min(link.Consumer.GetUnsatisfiedDemand(link.Item), node.GetUnusedOutput(item));
                    }
                }
            }

            foreach (ProductionNode node in Nodes)
            {
                foreach (Item item in node.Inputs)
                {
                    List<NodeLink> inLinksForThisItem = new List<NodeLink>();
                    foreach (NodeLink link in node.InputLinks)
                    {
                        link.Throughput += Math.Min(link.Consumer.GetUnsatisfiedDemand(link.Item), link.Supplier.GetUnusedOutput(item));
                    }
                }
            }
            */
        }

        public event EventHandler<IReadOnlyCollection<ProductionNode>?>? NodeValuesUpdated;

        private void UpdateModuleStrategy()
        {
            ModuleSelector.Default.Strategy = SelectedModuleStrategy;
        }

        public void UpdateNodeValues()
        {
            try {
                this.FindOptimalGraphToSatisfyFixedNodes();
            } catch (OverflowException) {
                //If the numbers here are so big they're causing an overflow, there's not much I can do about it. It's already pretty clear in the UI that the values are unusable.
                //At least this way it doesn't crash...
            }
            UpdateLinkThroughputs();
            NodeValuesUpdated?.Invoke(this, null);
        }

        public void UpdateNodeValues(ProductionNode node)
        {
            var nodeGroup = GetConnectedComponent(node);
            UpdateNodeValues(nodeGroup);
        }

        public void UpdateNodeValues(IReadOnlyCollection<ProductionNode> nodes)
        {
            try {
                this.FindOptimalGraphToSatisfyFixedNodes(nodes);
            } catch (OverflowException) {
                //If the numbers here are so big they're causing an overflow, there's not much I can do about it. It's already pretty clear in the UI that the values are unusable.
                //At least this way it doesn't crash...
            }
            UpdateLinkThroughputs();
            NodeValuesUpdated?.Invoke(this, nodes);
        }

        public void CreateAllPossibleInputLinks()
        {
            foreach (ProductionNode node in Nodes) {
                CreateAllLinksForNode(node);
            }
        }

        //Returns true if a new link was created
        public void CreateAllLinksForNode(ProductionNode node)
        {
            foreach (Item item in node.Inputs) {
                foreach (ProductionNode existingNode in Nodes.Where(n => n.Outputs.Contains(item))) {
                    if (existingNode != node) {
                        NodeLink.Create(existingNode, node, item);
                    }
                }
            }
        }

        //Returns any nodes that are created
        public IEnumerable<ProductionNode> CreateOrLinkAllPossibleRecipeNodes(ProductionNode node)
        {
            var createdNodes = new List<ProductionNode>();

            foreach (Item item in node.Inputs) {
                var recipePool =
                    item.Recipes.Where(r => !r
                        .IsCyclic); //Ignore recipes that can ultimately supply themselves, like filling/emptying barrels or certain modded recipes

                foreach (Recipe recipe in recipePool.Where(r => r.Enabled)) {
                    var existingNodes = Nodes.OfType<RecipeNode>().Where(n => n.BaseRecipe == recipe);

                    if (!existingNodes.Any()) {
                        RecipeNode newNode = RecipeNode.Create(recipe, this);
                        NodeLink.Create(newNode, node, item);
                        createdNodes.Add(newNode);
                    } else {
                        foreach (RecipeNode existingNode in existingNodes) {
                            NodeLink.Create(existingNode, node, item);
                        }
                    }
                }
            }

            return createdNodes;
        }

        //Returns any nodes that are created
        public IEnumerable<ProductionNode> CreateOrLinkAllPossibleSupplyNodes(ProductionNode node)
        {
            var createdNodes = new List<ProductionNode>();

            var unlinkedItems = node.Inputs.Where(i => node.InputLinks.All(nl => nl.Item != i));

            foreach (Item item in unlinkedItems) {
                var existingNodes = Nodes.OfType<SupplyNode>().Where(n => n.SuppliedItem == item);

                if (!existingNodes.Any()) {
                    SupplyNode newNode = SupplyNode.Create(item, this);
                    NodeLink.Create(newNode, node, item);
                    createdNodes.Add(newNode);
                } else {
                    foreach (SupplyNode existingNode in existingNodes) {
                        NodeLink.Create(existingNode, node, item);
                    }
                }
            }
            return createdNodes;
        }

        public IEnumerable<ProductionNode> GetInputlessNodes()
        {
            return Nodes.Where(x => !x.InputLinks.Any());
        }

        //https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm
        private class TarjanNode
        {
            public ProductionNode SourceNode { get; }
            public int Index { get; set; } = -1;
            public int LowLink { get; set; } = -1;
            public HashSet<TarjanNode> Links { get; } = new(); //Links to other nodes

            public TarjanNode(ProductionNode sourceNode)
            {
                SourceNode = sourceNode;
            }
        }

        //A strongly connected component is a set of nodes in a directed graph that each has a route to every other node in the set.
        //In this case it means there is a potential manufacturing loop e.g. emptying/refilling oil barrels
        //Each individual node counts as a SCC by itself, but we're only interested in groups so there is a parameter to ignore them
        public IEnumerable<IEnumerable<ProductionNode>> GetStronglyConnectedComponents(bool ignoreSingles)
        {
            var strongList = new List<List<ProductionNode>>();
            var S = new Stack<TarjanNode>();
            int indexCounter = 0;

            var tNodes = Nodes.ToDictionary(n => n, n => new TarjanNode(n));

            foreach (ProductionNode n in Nodes) {
                foreach (ProductionNode m in Nodes) {
                    if (m.InputLinks.Any(l => l.Supplier == n)) {
                        tNodes[n].Links.Add(tNodes[m]);
                    }
                }
            }

            foreach (TarjanNode v in tNodes.Values) {
                if (v.Index == -1) {
                    StrongConnect(strongList, S, indexCounter, v);
                }
            }

            if (ignoreSingles) {
                return strongList.Where(scc => scc.Count > 1);
            }
            return strongList;
        }

        public HashSet<ProductionNode> GetConnectedNodes(IEnumerable<ProductionNode> nodes)
        {
            var connected = new HashSet<ProductionNode>();
            var toVisitNext = new Stack<ProductionNode>(nodes);

            while (toVisitNext.Any()) {
                ProductionNode currentNode = toVisitNext.Pop();
                if (!connected.Add(currentNode))
                    continue;

                foreach (NodeLink link in currentNode.InputLinks)
                    toVisitNext.Push(link.Supplier);
                foreach (NodeLink link in currentNode.OutputLinks)
                    toVisitNext.Push(link.Consumer);
            }

            return connected;
        }

        public HashSet<ProductionNode> GetConnectedComponent(ProductionNode node)
        {
            var component = new HashSet<ProductionNode>();
            var toVisitNext = new Stack<ProductionNode>();
            toVisitNext.Push(node);

            while (toVisitNext.Any()) {
                ProductionNode currentNode = toVisitNext.Pop();
                if (!component.Add(currentNode))
                    continue;

                foreach (NodeLink link in currentNode.InputLinks)
                    toVisitNext.Push(link.Supplier);
                foreach (NodeLink link in currentNode.OutputLinks)
                    toVisitNext.Push(link.Consumer);
            }

            return component;
        }

        public IEnumerable<IReadOnlyCollection<ProductionNode>> GetConnectedComponents()
        {
            var unvisitedNodes = new HashSet<ProductionNode>(Nodes);

            var connectedComponents = new List<HashSet<ProductionNode>>();

            while (unvisitedNodes.Any()) {
                var component = new HashSet<ProductionNode>();
                connectedComponents.Add(component);
                var toVisitNext = new HashSet<ProductionNode>();
                toVisitNext.Add(unvisitedNodes.First());

                while (toVisitNext.Any()) {
                    ProductionNode currentNode = toVisitNext.First();

                    foreach (NodeLink link in currentNode.InputLinks) {
                        if (unvisitedNodes.Contains(link.Supplier)) {
                            toVisitNext.Add(link.Supplier);
                        }
                    }
                    foreach (NodeLink link in currentNode.OutputLinks) {
                        if (unvisitedNodes.Contains(link.Consumer)) {
                            toVisitNext.Add(link.Consumer);
                        }
                    }

                    component.Add(currentNode);
                    toVisitNext.Remove(currentNode);
                    unvisitedNodes.Remove(currentNode);
                }
            }

            return connectedComponents;
        }

        private void StrongConnect(List<List<ProductionNode>> strongList, Stack<TarjanNode> S, int indexCounter,
            TarjanNode v)
        {
            v.Index = indexCounter;
            v.LowLink = indexCounter++;
            S.Push(v);

            foreach (TarjanNode w in v.Links) {
                if (w.Index == -1) {
                    StrongConnect(strongList, S, indexCounter, w);
                    v.LowLink = Math.Min(v.LowLink, w.LowLink);
                } else if (S.Contains(w)) {
                    v.LowLink = Math.Min(v.LowLink, w.LowLink);
                }
            }

            {
                if (v.LowLink == v.Index) {
                    strongList.Add(new List<ProductionNode>());
                    TarjanNode w;
                    do {
                        w = S.Pop();
                        strongList.Last().Add(w.SourceNode);
                    } while (w != v);
                }
            }
        }

        public List<ProductionNode> GetTopologicalSort()
        {
            int[,] matrix = AdjacencyMatrix;
            List<ProductionNode> L = new(); //Final sorted list
            List<ProductionNode> S = GetInputlessNodes().ToList();

            while (S.Any()) {
                ProductionNode node = S.First();
                S.Remove(node);
                L.Add(node);

                int n = Nodes.IndexOf(node);

                for (int m = 0; m < Nodes.Count; m++) {
                    if (matrix[n, m] == 1) {
                        matrix[n, m] = 0;
                        int edgeCount = 0;
                        for (int i = 0; i < matrix.GetLength(1); i++) {
                            edgeCount += matrix[i, m];
                        }
                        if (edgeCount == 0) {
                            S.Insert(0, Nodes[m]);
                        }
                    }
                }
            }

            for (int i = 0; i < matrix.GetLength(0); i++) {
                for (int j = 0; j < matrix.GetLength(1); j++) {
                    // Edges mean there's a cycle somewhere and the sort can't be completed
                }
            }

            return L;
        }
    }
}
