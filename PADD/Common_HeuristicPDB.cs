using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    class PDBHeuristic : Heuristic
    {
        private List<int> variables;
        private Dictionary<int, HashSet<int>> edges;
        private List<HashSet<int>> components;
        private bool[] visited;
        private Dictionary<HashSet<int>, Dictionary<int[], double>> patternValues;

        public override string getDescription()
        {
            return "PDB heuristic";
        }

        public PDBHeuristic(SASProblem problem)
        {
            this.problem = problem;
            //findAdditivePatterns(problem);
        }

        /// <summary>
        /// Creates a database from given pattern
        /// </summary>
        /// <param name="selectedVariables"></param>
        /// <returns></returns>
        public void initializePatterns(HashSet<int> selectedVariables)
        {
            Console.WriteLine("Building the patterns database...");
            this.components = new List<HashSet<int>>();
            components.Add(selectedVariables);
            computeDistances();
        }

        //komponenty souvislosti. V grafu promennych budou spojenne prave kdyz existuje operator ktery je meni naraz
        private void findAdditivePatterns(SASProblem problem)
        {
            Console.WriteLine("Building the patterns database...");
            variables = new List<int>(problem.GetVariablesCount());
            visited = new bool[problem.GetVariablesCount()];
            edges = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < problem.GetVariablesCount(); i++)
            {
                variables.Add(i);
                edges.Add(i, new HashSet<int>());
            }
            foreach (SASOperator op in problem.GetOperators())
            {
                for (int i = 0; i < op.GetEffects().Count; i++)
                {
                    for (int j = i+1; j < op.GetEffects().Count; j++)
                    {
                        edges[op.GetEffects()[i].GetEff().variable].Add(op.GetEffects()[j].GetEff().variable);
                        edges[op.GetEffects()[j].GetEff().variable].Add(op.GetEffects()[i].GetEff().variable);
                    }
                }
            }
            findComponents();
            deleteNonGoalComponents();
            Console.WriteLine(components.Count + " patterns found.");
            computeDistances();
            Console.WriteLine("Done");
        }

        private void computeDistances()
        {
            patternValues = new Dictionary<HashSet<int>, Dictionary<int[], double>>();
            foreach (var item in components)
            {
                Console.Write("Computing pattern ");
                foreach (var ff in item)
                    Console.Write(ff + " ");
                Console.WriteLine();
                patternValues.Add(item, new Dictionary<int[], double>(new ArrayEqualityComparer()));
                SASStateAbstracted.SetNotAbstractedVariables(item);
                computeDistancesToGoal(item);
            }
        }

        private int[] getValues(IState s)
        {
            return ((SASState)s).GetAllValues();
        }

        private void computeDistancesToGoal(HashSet<int> item)
        {
            IHeap<double, IState> fringe = new Heaps.LeftistHeap<IState>();
            insertAllGoalStates(fringe, item);
            long stateSpaceSize = 1;
            foreach (var varr in item)
                stateSpaceSize *= problem.GetVariableDomainRange(varr);
            
            Console.WriteLine("Pattern's size: " + item.Count +" state space size: " + stateSpaceSize);

            while (fringe.size() > 0)
            {
                double stateDistance = fringe.getMinKey();
                IState state = fringe.removeMin();
                int[] stateValues = getValues(state);
                if (patternValues[item].ContainsKey(stateValues))
                {
                    continue;
                }
                patternValues[item].Add(stateValues, stateDistance);
                //List<HashSet<int>> predecessors = new List<HashSet<int>>();
                var preds = problem.GetAllPredecessors(state);

                foreach (var pred in preds)
                {
                    //HashSet<int> preValues = getValues(pre[op]);
                    if (!patternValues[item].ContainsKey(((SASState)pred.GetPredecessorState()).GetAllValues()))
                        fringe.insert(stateDistance + pred.GetOperator().GetCost(), pred.GetPredecessorState());
                }
            }
        }

        private void insertAllGoalStates(IHeap<double, IState> fringe, HashSet<int> patternVariables)
        {
            insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(-1), new List<int>());
        }

        private void insertAllGoalStates(IHeap<double, IState> fringe, HashSet<int> patternVariables, int currentVariable, List<int> values)
        {
            if (values.Count == patternVariables.Count)
            {
                SASStateAbstracted s = new SASStateAbstracted(problem);
                int i = 0;
                foreach (var item in SASStateAbstracted.notAbstractedVariablesIndices.Keys)
                {
                    s.SetValue(item, values[i]);
                    i++;
                }
                fringe.insert(0, s);
                return;
            }
            if (problem.GetGoalConditions().IsVariableAffected(currentVariable))
            {
                values.Add(problem.GetGoalConditions().GetGoalValueForVariable(currentVariable));
                insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(currentVariable), values);
                values.RemoveAt(values.Count - 1);
                return;
            }
            for (int i = 0; i < problem.GetVariableDomainRange(currentVariable); i++)
            {
                values.Add(i);
                insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(currentVariable), values);
                values.RemoveAt(values.Count - 1);
            }

        }

        private int getNextNotAbstractedVariable(int currentVar)
        {
            bool found = currentVar < 0;
            foreach (var item in SASStateAbstracted.notAbstractedVariablesIndices.Keys)
            {
                if (found)
                    return item;
                if (item == currentVar)
                    found = true;
            }
            return -1;
        }

        private void deleteNonGoalComponents()
        {
            components.RemoveAll(a => !(problem.GetGoalConditions().Any(b => a.Contains(b.variable))));
            //components.RemoveAll(a => !(intersectsGoal(a)));
        }

        private bool intersectsGoal(HashSet<int> component)
        {
            return problem.GetGoalConditions().Any(a => component.Contains(a.variable));
        }

        private void findComponents()
        {
            components = new List<HashSet<int>>();
            for (int i = 0; i < problem.GetVariablesCount(); i++)
            {
                if (!visited[i])
                {
                    components.Add(new HashSet<int>());
                    addAllReachable(i, components.Count - 1);
                }
            }
        }

        private void addAllReachable(int variable, int componentNumber)
        {
            if (!visited[variable])
            {
                visited[variable] = true;
                components[componentNumber].Add(variable);
                foreach (var item in edges[variable])              
                    addAllReachable(item, componentNumber);  
            }
        }

        protected override double evaluate(IState state)
        {
            SASState sasState = (SASState)state;

            double result = 0;
            foreach (var item in patternValues.Keys)
            {
                int[] notAbstractedValues = new int[item.Count];
                int i = 0;
                foreach (var variable in item)
                {
                    notAbstractedValues[i++] = sasState.GetValue(variable);
                }
                if (!patternValues[item].ContainsKey(notAbstractedValues))
                    return int.MaxValue;
                result += patternValues[item][notAbstractedValues];
            }
            return result;
        }
    }
}
