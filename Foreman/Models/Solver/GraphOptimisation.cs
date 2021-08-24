namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class GraphOptimisations
    {
        public static void FindOptimalGraphToSatisfyFixedNodes(this ProductionGraph graph)
        {
            foreach (ProductionNode node in graph.Nodes.Where(n => n.RateType == RateType.Auto)) {
                node.ResetSolvedRate();
            }

            foreach (var nodeGroup in graph.GetConnectedComponents()) {
                OptimiseNodeGroup(nodeGroup);
            }

            graph.UpdateLinkThroughputs();
        }

        public static void FindOptimalGraphToSatisfyFixedNodes(
            this ProductionGraph graph, IReadOnlyCollection<ProductionNode> nodeGroup)
        {
            foreach (ProductionNode node in nodeGroup.Where(n => n.RateType == RateType.Auto)) {
                node.ResetSolvedRate();
            }

            OptimiseNodeGroup(nodeGroup);

            graph.UpdateLinkThroughputs();
        }

        public static void OptimiseNodeGroup(IReadOnlyCollection<ProductionNode> nodeGroup)
        {
            if (nodeGroup.Count == 0)
                return;

            var solver = new ProductionSolver();

            foreach (var node in nodeGroup) {
                node.AddConstraints(solver);
            }

            var solution = solver.Solve();

            Debug.WriteLine(solver.ToString());

            // TODO: Handle BIG NUMBERS
            // TODO: Return error in solution!?
            if (solution == null)
                throw new Exception("Solver failed but that shouldn't happen.\n" + solver);

            foreach (var node in nodeGroup) {
                node.SetSolvedRate(solution.ActualRate(node));
                foreach (var link in node.OutputLinks.Union(node.InputLinks)) {
                    link.Throughput = solution.Throughput(link);
                }
            }
        }
    }

    // Using partial classes here to group all the constraints related code into this file so it's
    // easy to understand as a whole.
    public abstract partial class ProductionNode
    {
        internal void ResetSolvedRate()
        {
            ActualRate = 0;
        }

        internal virtual void SetSolvedRate(double rate)
        {
            ActualRate = (float)rate;
        }

        internal void AddConstraints(ProductionSolver solver)
        {
            solver.AddNode(this);

            double? rate = GetActualDesiredRate();
            if (rate != null)
                solver.AddTarget(this, (float)rate.Value);

            foreach (var itemInputs in InputLinks.GroupBy(x => x.Item)) {
                var item = itemInputs.Key;

                solver.AddInputRatio(this, item, itemInputs, InputRateFor(item));
                solver.AddInputLink(this, item, itemInputs, InputRateFor(item));
            }

            foreach (var itemOutputs in OutputLinks.GroupBy(x => x.Item)) {
                var item = itemOutputs.Key;

                solver.AddOutputRatio(this, item, itemOutputs, OutputRateFor(item));
                // Output links do not need to constrained, since they are already covered by adding
                // the input link above.
            }
        }

        internal abstract double OutputRateFor(Item item);
        internal abstract double InputRateFor(Item item);
    }
}
