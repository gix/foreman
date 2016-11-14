﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Foreman
{
	public static class GraphOptimisations
	{
		public static void FindOptimalGraphToSatisfyFixedNodes(this ProductionGraph graph)
		{
			List<ProductionNode> relevantNodes = graph.Nodes;
			int nodeCount = relevantNodes.Count();
			Dictionary<Item, Decimal> itemRequirements = new Dictionary<Item, decimal>();

			foreach (var node in graph.Nodes)
			{
				foreach (Item item in node.Inputs.Union(node.Outputs))
				{
					itemRequirements[item] = 0M;
				}
			}

			foreach (var node in graph.Nodes.Where(n => n.rateType == RateType.Manual))
			{
				foreach (Item item in node.Inputs.Concat(node.Outputs))
				{
					itemRequirements[item] += (decimal)node.GetTotalDemand(item) - (decimal)node.GetDesiredOutput(item);
				}
			}

			LinearProgrammingSolver solver = new LinearProgrammingSolver();
			foreach (Item item in itemRequirements.Keys)
			{
				decimal[] equationCoefficients = new decimal[nodeCount];
				for (int i = 0; i < relevantNodes.Count(); i++)
				{
					ProductionNode node = relevantNodes[i];
					if (node is SupplyNode && node.Outputs.Contains(item))
					{
						equationCoefficients[i] = 1;
					} else if (node is ConsumerNode && node.Inputs.Contains(item))
					{
						equationCoefficients[i] = -1;
					}
					else if (node is RecipeNode)
					{
						if (node.Inputs.Contains(item))
						{
							equationCoefficients[i] -= (decimal)((RecipeNode)node).BaseRecipe.Ingredients[item];
						}
						if (node.Outputs.Contains(item))
						{
							equationCoefficients[i] += (decimal)((RecipeNode)node).BaseRecipe.Results[item];
						}
					}
				}

				if (itemRequirements[item] < 0)
				{
					solver.AddConstraint(new Constraint(-itemRequirements[item], ConstraintType.LessThan, equationCoefficients.Select(c => -c).ToArray()));
				}
				else
				{
					solver.AddConstraint(new Constraint(itemRequirements[item], ConstraintType.GreaterThan, equationCoefficients));
				}
			}
			
			decimal[] objectiveFunctionCoefficients = new decimal[nodeCount];
			{
				int i = 0;
				foreach (ProductionNode node in relevantNodes)
				{
					if (node is SupplyNode)
					{
						if (((SupplyNode)node).Outputs.Contains(DataCache.Items["water"]))
						{
							objectiveFunctionCoefficients[i] = 0M;
						}
						else
						{
							objectiveFunctionCoefficients[i] = 1M;
						}						
					} else if (node is ConsumerNode)
					{
						objectiveFunctionCoefficients[i] = -1M;
					}
					i++;
				}
			}

			solver.SetObjectiveFunction(objectiveFunctionCoefficients, ObjectiveFunctionType.Minimise);

			var solution = solver.solve();
			for (int i = 0; i < relevantNodes.Count(); i++)
			{
				if (relevantNodes[i].rateType == RateType.Auto)
				{
					relevantNodes[i].desiredRate = Convert.ToSingle(solution[i]);
				}
				if (!(relevantNodes[i] is ConsumerNode) && relevantNodes[i].rateType == RateType.Auto)
				{
					relevantNodes[i].actualRate = Convert.ToSingle(solution[i]);
				}
			}

			graph.UpdateLinkThroughputs();
		}
	}
}