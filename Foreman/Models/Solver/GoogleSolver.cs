﻿using System;
using Google.OrTools.LinearSolver;
using System.Collections.Generic;
using System.Text;

namespace Foreman
{
    // A super thin wrapper around OrTools.LinearSolver to make up for its deficiences as a generated class.
    public class GoogleSolver
    {
        private Solver solver;
        private List<Variable> variables;
        private List<Constraint> constraints;

        public static GoogleSolver Create()
        {
            return new GoogleSolver();
        }

        public GoogleSolver()
        {
            this.solver = Solver.CreateSolver("Foreman", "GLOP_LINEAR_PROGRAMMING");
            this.variables = new List<Variable>();
            this.constraints = new List<Constraint>();
        }

        public void PrintTo(StringBuilder buffer)
        {
            var objective = solver.Objective();

            buffer.AppendLine("objective:");
            buffer.AppendFormat("  {0}(", objective.Minimization() ? "min" : "max");

            int i = 0;
            foreach (var variable in variables)
            {
                double coefficient = objective.GetCoefficient(variable);
                if (coefficient == 0)
                    continue;

                if (i > 0)
                    buffer.Append(" + ");
                buffer.Append(coefficient);
                buffer.Append(' ');
                buffer.Append(variable.Name());
                ++i;
            }
            buffer.AppendLine(")");
            buffer.AppendLine();

            buffer.AppendLine("constraints:");
            foreach (var constraint in constraints)
            {
                i = 0;
                buffer.Append("  ");
                foreach (var variable in variables)
                {
                    double coefficient = constraint.GetCoefficient(variable);
                    if (coefficient == 0)
                        continue;

                    if (i > 0)
                        buffer.Append(" + ");
                    buffer.Append(coefficient);
                    buffer.Append(' ');
                    buffer.Append(variable.Name());
                    ++i;
                }

                if (double.IsPositiveInfinity(constraint.Ub()))
                    buffer.AppendFormat(" ≥ {0}", constraint.Lb());
                else if (double.IsNegativeInfinity(constraint.Lb()))
                    buffer.AppendFormat(" ≤ {0}", constraint.Ub());
                else
                    buffer.AppendFormat(" ∈ [{0}, {1}]", constraint.Lb(), constraint.Ub());

                buffer.AppendLine();
            }
            buffer.AppendLine();

            buffer.AppendLine("solution:");
            foreach (var variable in variables)
            {
                buffer.AppendFormat("  {0} = {1}", variable.Name(), variable.SolutionValue());
                buffer.AppendLine();
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            PrintTo(buffer);
            return buffer.ToString();
        }

        internal Objective Objective()
        {
            return solver.Objective();
        }

        internal int Solve()
        {
            return solver.Solve();
        }

        internal Constraint MakeConstraint(double low, double high)
        {
            var constraint = solver.MakeConstraint(low, high);
            this.constraints.Add(constraint);
            return constraint;
        }

        internal Variable MakeNumVar(double low, double high, string name)
        {
            var variable = solver.MakeNumVar(low, high, name);
            this.variables.Add(variable);
            return variable;
        }
    }
}