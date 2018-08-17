using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace PADD
{
    class KnowledgeExtraction
    {
        public static CausualGraph computeCausualGraph(SASProblem problem)
        {
            CausualGraph result = new CausualGraph();
            result.vertices = new List<int>(problem.GetVariablesCount());
            result.isEdge = new bool[problem.GetVariablesCount(), problem.GetVariablesCount()];
			result.isMentionedInGoal = new bool[problem.GetVariablesCount()];
            for (int i = 0; i < problem.GetVariablesCount(); i++)
            {
                result.vertices.Add(i);
				result.isMentionedInGoal[i] = problem.GetGoalConditions().Any(g => g.variable == i);
            }

            foreach (SASOperator item in problem.GetOperators())
            {
                foreach (var precond in item.GetPreconditions())
                {
                    foreach (var eff in item.GetEffects())
                    {
                        if (eff.GetEff().variable != precond.variable)
                            result.setEdge(precond.variable, eff.GetEff().variable);
                    }
                }
                foreach (var eff in item.GetEffects())
                {
                    foreach (var cond in eff.GetConditions())
                    {
                        if (eff.GetEff().variable != cond.variable)
                            result.setEdge(cond.variable, eff.GetEff().variable);
                    }
                }
                foreach (var eff in item.GetEffects())
                {
                    foreach (var eff2 in item.GetEffects())
                    {
                        if (eff.GetEff().variable != eff2.GetEff().variable)
                            result.setEdge(eff.GetEff().variable, eff2.GetEff().variable);
                    }
                }
            }

            return result;
        }

        public static DomainTransitionGraph computeDTG(SASProblem problem, int variable)
        {
            DomainTransitionGraph result = new DomainTransitionGraph();
            result.variable = variable;
            result.vertices = new List<int>();
            result.edges = new List<GraphEdge>();

			if (problem.GetGoalConditions().Any(g => g.variable == variable))
				result.goalValue = problem.GetGoalConditions().Where(g => g.variable == variable).Single().value;
			for (int i = 0; i < problem.GetVariableDomainRange(variable); i++)
            {
                result.vertices.Add(i);
            }
            foreach (SASOperator item in problem.GetOperators())
            {
                foreach (var eff in item.GetEffects())
                {
                    if (eff.GetEff().variable == variable)
                    {
                        int targetValue = eff.GetEff().value;
                        int originalValue = -1;

                        List<SASVariableValuePair> listOutsideConditions = new List<SASVariableValuePair>();
                        List<SASVariableValuePair> listOutsideEffects = new List<SASVariableValuePair>();

                        foreach (var eff1 in item.GetEffects())
                        {
                            if (eff1.GetEff().variable != eff.GetEff().variable)
                                listOutsideEffects.Add(eff1.GetEff());
                        }

                        foreach (var effCond in eff.GetConditions())
                        {
                            if (effCond.variable == variable)
                                originalValue = effCond.value;
                            else
                                listOutsideConditions.Add(new SASVariableValuePair(effCond.variable, effCond.value));
                        }

                        foreach (var precond in item.GetPreconditions())
                        {
                            if (precond.variable == variable)
                                originalValue = precond.value;
                            else
                                listOutsideConditions.Add(new SASVariableValuePair(precond.variable, precond.value));
                        }

                        if (originalValue != -1)
                        {
                            GraphEdge e = new GraphEdge();
                            e.from = originalValue;
                            e.to = targetValue;
                            e.outsideCondition = new SASOperatorEffect(new SASOperatorPreconditions(listOutsideConditions), new SASVariableValuePair(-1,-1));
                            e.outsideEffect = new SASOperatorEffect(new SASOperatorPreconditions(listOutsideEffects), new SASVariableValuePair(-1, -1));
                            e.op = item;
                            result.edges.Add(e);
                        }
                        else
                        {
                            foreach (var val in result.vertices)
                            {
                                if (val == targetValue)
                                    continue;
                                GraphEdge e = new GraphEdge();
                                e.from = val;
                                e.to = targetValue;
                                e.outsideCondition = new SASOperatorEffect(new SASOperatorPreconditions(listOutsideConditions), new SASVariableValuePair(-1, -1));
                                e.outsideEffect = new SASOperatorEffect(new SASOperatorPreconditions(listOutsideEffects), new SASVariableValuePair(-1, -1));
                                e.op = item;
                                result.edges.Add(e);
                            }
                        }
                    }
                }
            }
            result.computeRSE_Invertibility();
            return result;
        }

		public static PredicateConstantGraph computeObjectGraph(SASProblem p)
		{
			//var pddlProblemPath = Utils.FileSystemUtils.getPDDLProblemPath(p.GetInputFilePath());
			//var pp = PDDLProblem.CreateFromFile(pddlProblemPath.domainFile, pddlProblemPath.problemFile);

			var t = translateSASProblemToPDDL(p);
			var pp = t;
			return new PredicateConstantGraph(pp);
		}

		/// <summary>
		/// Gets SaSproblem and produces a PDDLProblem description that
		/// is equivalent to current SASState. It returns content of a PDDL problem file (that when parsed would have the same initial state as the SaSProblem.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		public static PDDLProblem translateSASProblemToPDDL(SASProblem s)
		{
			var pddlProblemPath = Utils.FileSystemUtils.getPDDLProblemPath(s.GetInputFilePath());
			string originalText = System.IO.File.ReadAllText(pddlProblemPath.problemFile);

			string PDDLStateInitRegion = originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).First().
				Split(new string[] { "(:" }, StringSplitOptions.RemoveEmptyEntries).First();
			List<string> predicates = PDDLStateInitRegion.Split('(').Select(r => r.Replace(")", "").Trim()).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
			predicates = predicates.Select(q => { int firstSpace = q.IndexOf(" "); return q.Remove(firstSpace, 1).Insert(firstSpace, "("); }).Select(q => q.Replace(" ", ", ") + ")").ToList();

			List<string> newPredicates = new List<string>();
			var initialState = (SASState)s.GetInitialState();
			for (int i = 0; i < initialState.GetAllValues().Length; i++)
			{
				List<string> corresponding = predicates.Where(q => s.GetVariablesData()[i].valuesSymbolicMeaning.Contains("Atom " + q)).ToList();
				predicates.RemoveAll(p => corresponding.Contains(p));
				newPredicates.Add(s.GetVariablesData()[i].valuesSymbolicMeaning[initialState.GetAllValues()[i]]);
			}
			predicates.AddRange(newPredicates);
			predicates = predicates.Select(p => p.Replace("Atom ", "").Replace("(", " ").Replace(",", "").Replace(")", "").Trim()).ToList();
			string tempFileName = System.IO.Path.GetTempFileName();

			string text = originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).First() + "(:init\n";
			text += string.Join("\n", predicates.Select(q => "\t(" + q + ")"));
			text += "\n)\n(:";
			text += string.Join("(:", originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).First().Split(new string[] { "(:" }, StringSplitOptions.RemoveEmptyEntries).Skip(1));

			System.IO.File.WriteAllText(tempFileName, text);

			PDDLProblem result = PDDLProblem.CreateFromFile(pddlProblemPath.domainFile, tempFileName);
			System.IO.File.Delete(tempFileName);
			return result;
		}

	}



    public class KnowledgeHolder
    {
        public CausualGraph CG;
        public List<DomainTransitionGraph> DTGs;
		public PredicateConstantGraph predConstGraph;
		public HashSet<int> RSE_InvertibleVariables;

        public static KnowledgeHolder compute(SASProblem problem)
        {
            KnowledgeHolder result = new KnowledgeHolder();
            result.CG = KnowledgeExtraction.computeCausualGraph(problem);
            result.DTGs = new List<DomainTransitionGraph>();
            result.RSE_InvertibleVariables = new HashSet<int>();
            for (int i = 0; i < problem.GetVariablesCount(); i++)
            {
                result.DTGs.Add(KnowledgeExtraction.computeDTG(problem, i));
                if (result.DTGs[result.DTGs.Count - 1].isRSE_Invertible)
                    result.RSE_InvertibleVariables.Add(i);
            }
            return result;
        }

		public static KnowledgeHolder create(PDDLProblem problem)
		{
			KnowledgeHolder h = new KnowledgeHolder();
			h.predConstGraph = new PredicateConstantGraph(problem);
			return h;
		}

		private KnowledgeHolder()
		{

		}

        public void show(int variable, System.Windows.Forms.Panel panel)
        {
			if (predConstGraph != null)
			{
				predConstGraph.visualize(panel);
				return;
			}

			if (variable == 0)
            {
                CG.visualize(panel, RSE_InvertibleVariables);
                return;
            }
            if (variable <= CG.vertices.Count)
            {
                DTGs[variable - 1].visualize(true, panel);
                return;
            }
            DTGs[variable - CG.vertices.Count - 1].visualize(false, panel);
        }

        public void visualize(bool isSAS = true)
        {
			KnowledgeVisualizerForm f = new KnowledgeVisualizerForm();
			if (isSAS && CG != null)
			{
				f.listView1.Items.Add("Causual Graph");
				for (int i = 0; i < CG.vertices.Count; i++)
				{
					f.listView1.Items.Add("DTG var" + i.ToString());
				}
				for (int i = 0; i < CG.vertices.Count; i++)
				{
					f.listView1.Items.Add("DTG NoLabel var" + i.ToString());
				}

			}
			else
			{
				f.listView1.Items.Add("Object graph");
			}
			f.h = this;
			System.Windows.Forms.Application.Run(f);
		}
    }

    public class CausualGraph
    {
        public List<int> vertices;
        public bool[,] isEdge;
        private bool hasSomeEdge = false;
		public bool[] isMentionedInGoal;

        public void setEdge(int from, int to)
        {
            isEdge[from, to] = true;
            hasSomeEdge = true;
        }

        public void visualize(System.Windows.Forms.Panel panel = null, HashSet<int> invertibleVariables = null)
        {
			Graph g = new Graph("Causual Graph");
            foreach (var item in vertices)
            {
                var node = g.AddNode(item.ToString());
                if (invertibleVariables != null && !invertibleVariables.Contains(item))
                {
					node.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                }
				if (isMentionedInGoal[item])
					node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Yellow;
			}
            for (int i = 0; i < isEdge.GetLength(0); i++)
                for (int j = 0; j < isEdge.GetLength(1); j++)
                    if (isEdge[i, j]) g.AddEdge(i.ToString(), j.ToString());

			GViewer viewer = new GViewer();
            viewer.Graph = g;
			viewer.CurrentLayoutMethod = LayoutMethod.MDS;
            if (panel == null)
            {
                KnowledgeVisualizerForm form = new KnowledgeVisualizerForm();
                form.SuspendLayout();
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                form.Controls.Add(viewer);
                form.ResumeLayout();
                System.Windows.Forms.Application.Run(form);
            }
            else
            {
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                panel.Controls.Clear();
                panel.Controls.Add(viewer);
            }
        }

        public CausualGraph projection(HashSet<int> variables)
        {
            CausualGraph result = new CausualGraph();
            result.vertices = new List<int>();
            int max = 0;
            foreach (var item in this.vertices)
            {
                if (variables.Contains(item))
                {
                    result.vertices.Add(item);
                    if (max < item) 
                        max = item;
                }
            }
            result.isEdge = new bool[max, max];
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    if (this.isEdge[i, j]) 
                        result.setEdge(i, j);
                }
            }
            return result;
        }

        private class acyclicityChecker
        {
            // 0 = not visited, 1 = pending, 2 = closed
            Dictionary<int, int> visited = new Dictionary<int, int>(),
                enterTime = new Dictionary<int, int>(),
                exitTime = new Dictionary<int, int>();
            
            int time = 0;
            CausualGraph gr;
            bool hasCycle = false;

            private void doDFS(CausualGraph g)
            {
                hasCycle = false;
                visited.Clear();
                enterTime.Clear();
                exitTime.Clear();
                gr = g;
                foreach (var item in g.vertices)
                    visited.Add(item, 0);
                time = 0;
                foreach (var item in g.vertices)
                {
                    if (visited[item] == 0)
                        visit(item);
                }
            }

            public bool isAcyclic(CausualGraph g)
            {
                doDFS(g);
                return !hasCycle;
            }

            private void visit(int vertex)
            {
                visited[vertex] = 1;
                time++;
                enterTime[vertex] = time;
                foreach (var successor in gr.vertices)
                {
                    if (gr.isEdge[vertex, successor])
                    {
                        if (visited[successor] == 0)
                        {
                            visit(successor);
                        }
                        else
                        {
                            if (visited[successor] == 1)
                            {
                                hasCycle = true;
                            }
                        }

                    }
                }
                visited[vertex] = 3;
                time++;
                exitTime[vertex] = time;
            }
        }

    //    for i:=1 to n do barva[i]:=bílá;
    //čas:=0;
    //for i:=1 to n do if barva[i]=bílá then NAVŠTIV(i)

    //    NAVŠTIV(i) 
    //begin	barva[i]:=šedá; čas:=čas+1; d[i]:=čas;
    //for each j je soused i do 
    //if barva[j]=bílá 
    //    then 	begin	NAVŠTIV(j);
    //            označ (i,j) jako stromovou
    //        end
    //    else if barva[j]=šedá 
    //        then 	begin 	ohlas nalezení cyklu;
    //                označ (i,j) jako zpětnou
    //            end
    //        else if d[i] < d[j] 
    //            then označ (i,j) jako dopřednou
    //            else označ (i,j) jako příčnou
    //barva[i]:=černá; čas:=čas+1; f[i]:=čas
    //end;

        public bool isAcyclic()
        {
            acyclicityChecker checker = new acyclicityChecker();
            return checker.isAcyclic(this);
        }

        public List<int> topologicalOrder()
        {
            if (isEmpty())
            {
                return vertices;
            }
            else throw new Exception("Causual graph is not empty");
        }

        public bool isEmpty()
        {
            return !hasSomeEdge;
        }
    }

    public class DomainTransitionGraph
    {
        public int variable;
        public List<int> vertices;
        public List<GraphEdge> edges;
		public int goalValue = -1;
        
        private List<GraphEdge>[] edgesByVertices;
        private bool isTransformed = false;

        private void transformToSuccesorsLists()
        {
            this.edgesByVertices = new List<GraphEdge>[vertices.Count];
            foreach (var item in edges)
            {
                if (edgesByVertices[item.from] == null)
                    edgesByVertices[item.from] = new List<GraphEdge>();
                edgesByVertices[item.from].Add(item);
            }
            isTransformed = true;
        }
        
        public bool isRSE_Invertible = false;

        public void computeRSE_Invertibility()
        {
            isRSE_Invertible = true;
            foreach (var item in edges)
            {
                if (!isEdgeRSE_Invertible(item))
                {
                    isRSE_Invertible = false;
                    return;
                }
            }
        }

        private bool isEdgeRSE_Invertible(GraphEdge e)
        {
            if (e.isInvertibilityComputed)
                return e.isRSE_Invertible;
            e.isInvertibilityComputed =true;
            bool isJ_thConditionMet = false;
            foreach (var item in edges)
            {
                if (item.from != e.to || item.to != e.from)
                    continue;
                if (item.outsideCondition.GetConditions().Count == 0)
                {
                    e.isRSE_Invertible = true;
                    return true;
                }
                for (int j = 0; j < item.outsideCondition.GetConditions().Count; j++)
			    {
                    isJ_thConditionMet = false;
                    for (int i = 0; i < e.outsideCondition.GetConditions().Count; i++)
                    {
                        if (e.outsideCondition.GetConditions()[i].variable == item.outsideCondition.GetConditions()[j].variable &&
                            e.outsideCondition.GetConditions()[i].value == item.outsideCondition.GetConditions()[j].value)
                        {
                            isJ_thConditionMet = true;
                            break;
                        }
                    }
                    if (isJ_thConditionMet)
                        continue;
                    for (int i = 0; i < e.outsideEffect.GetConditions().Count; i++)
                    {
                        if (e.outsideEffect.GetConditions()[i].variable == item.outsideCondition.GetConditions()[j].variable &&
                            e.outsideEffect.GetConditions()[i].value == item.outsideCondition.GetConditions()[j].value)
                        {
                            isJ_thConditionMet = true;
                            break;
                        }
                    }
                    if (!isJ_thConditionMet)
                        break;
                }
                if (isJ_thConditionMet)
                {
                    e.isRSE_Invertible = true;
                    return true;
                }
            }
            e.isRSE_Invertible = false;
            return false;
        }

        public void visualize(bool isLabeled = true, System.Windows.Forms.Panel panel = null)
        {
            Microsoft.Msagl.Drawing.Graph g = new Microsoft.Msagl.Drawing.Graph("PlanningProblem Transition Graph of variable " + variable);
            foreach (var item in vertices)
            {
                var node = g.AddNode(item.ToString());
				if (item == goalValue)
					node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Yellow;
            }
            if (isLabeled)
                foreach (var item in edges)
                {
                    var e = g.AddEdge(item.from.ToString(), "Cond:" + item.outsideCondition.ToStringEffectCondition() + "Eff:" + item.outsideEffect.ToStringEffectCondition(), item.to.ToString());
                    if (!isEdgeRSE_Invertible(item))
                    {
                        e.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                    }
                }
            else
            {
                bool[,] isEdge = new bool[vertices.Count, vertices.Count];
                foreach (var item in edges)
                {
                    if (isEdge[item.from, item.to])
                        continue;
					var e = g.AddEdge(item.from.ToString(), item.to.ToString());
                    if (!isEdgeRSE_Invertible(item))
                    {
                        e.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                    }
                    isEdge[item.from, item.to] = true;
                }
            }

            Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            viewer.Graph = g;
            if (panel == null)
            {
                KnowledgeVisualizerForm form = new KnowledgeVisualizerForm();
                form.SuspendLayout();
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                form.Controls.Add(viewer);
                form.ResumeLayout();
                System.Windows.Forms.Application.Run(form);
            }
            else
            {
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                panel.Controls.Clear();
                panel.Controls.Add(viewer);
            }
        }

        /// <summary>
        /// Finds a path in DTG from given value to another value. Should be called only for black variables, and no other black variables should occur
        /// in the outside conditions. The red variables may occur and an edge is accessible only if the outisde condition is met by given RedValues.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="redValues"></param>
        /// <returns></returns>
        public List<IOperator> findPath(int from, int to, Dictionary<int, HashSet<int>> redValues)
        {
            if (!isTransformed)
                transformToSuccesorsLists();

            HashSet<int> visited = new HashSet<int>();
            IHeap<double, int> nodes = new Heaps.BinomialHeap<int>();
            int[] lengths = new int[vertices.Count], previous = new int[vertices.Count];
            IOperator[] previousOperator = new IOperator[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                lengths[i] = int.MaxValue;
            }

            lengths[from] = 0;
            nodes.insert(0, from);
            visited.Add(from);
            while (nodes.size() > 0)
            {
                int current = nodes.removeMin();
                if (current == to)
                    break;
                foreach (var item in edgesByVertices[current])
                {
                    if (isOutsideConditionMet(item.outsideCondition, redValues))
                    {
                        int succesor = item.to;
                        int newLength = lengths[current] + item.op.GetCost();
                        if (newLength < lengths[succesor])
                        {
                            lengths[succesor] = newLength;
                            previous[succesor] = current;
                            previousOperator[succesor] = item.op;
                            if (visited.Contains(succesor))
                            {
                                nodes.insert(lengths[succesor], succesor);
                            }
                        }
                        if (!visited.Contains(succesor))
                        {
                            visited.Add(succesor);
                            nodes.insert(lengths[succesor], succesor);
                        }
                    }
                }
            }

            List<IOperator> result = new List<IOperator>();

            int currentVal = to;
            while (currentVal != from)
            {
                result.Insert(0, previousOperator[currentVal]);
                currentVal = previous[currentVal];
            }

            return result;
        }

        private bool isOutsideConditionMet(SASOperatorEffect condition, Dictionary<int, HashSet<int>> redValues)
        {
            foreach (var cond in condition.GetConditions())
            {
                if (!redValues.ContainsKey(cond.variable))
                {
                    throw new Exception("Outside condition contains a black variable.");
                }
                if (!redValues[cond.variable].Contains(cond.value))
                    return false;
            }
            return true;
        }
    
    }

	public abstract class GraphVisualizable
	{
		public abstract Graph toMSAGLGraph();

		public void visualize(System.Windows.Forms.Panel panel)
		{
			Graph g = toMSAGLGraph();

			GViewer viewer = new GViewer();
			viewer.Graph = g;
			viewer.CurrentLayoutMethod = LayoutMethod.MDS;
			viewer.Dock = System.Windows.Forms.DockStyle.Fill;
			panel.Controls.Clear();
			panel.Controls.Add(viewer);
			panel.Refresh();
		}
	}

	public class PredicateConstantGraph : GraphVisualizable
	{
		PDDLProblem problem;

		public PredicateConstantGraph(PDDLProblem p)
		{
			this.problem = p;
		}

		protected bool addTypes(Graph g)
		{
			var typesManager = problem.GetIDManager().GetTypesMapping();
			if (typesManager.GetAllTypeIDs().Count() <= 1)
				return false;
			foreach (var item in typesManager.GetAllTypeIDs())
			{
				var node = g.AddNode(getTypeLabel(item, typesManager));
				formatAsTypeNode(node);
			}

			foreach (var parentType in typesManager.GetAllTypeIDs())
			{
				var childrenTypes = typesManager.GetChildrenTypeIDs(parentType);
				foreach (var childrenType in childrenTypes)
				{
					var edge = g.AddEdge(getTypeLabel(childrenType, typesManager), "subtype", getTypeLabel(parentType, typesManager));
					formatAsTypeEdge(edge);
					
				}
			}
			return true;
		}

		protected void addConstants(Graph g, bool useTyping)
		{
			var mapping = problem.GetIDManager().GetConstantsMapping();
			foreach (var item in mapping.GetConstantsIDs())
			{
				var node = g.AddNode(getConstLabel(item, mapping));
				node.Attr.FillColor = Color.Red;
				if (useTyping)
				{
					var edge = g.AddEdge(node.Id, "hasType", getTypeLabel(mapping.GetTypeID(mapping.GetStringForConstID(item)), problem.GetIDManager().GetTypesMapping()));
					formatAsTypeEdge(edge);
				}
			}
		}

		protected string getPredicateHeadID(IPDDLDesignator predicate)
		{
			return getPredicateHeadID(predicate.GetPrefixID());
		}

		protected string getPredicateHeadID(int predicateSymbolID)
		{
			var label = problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(predicateSymbolID);
			return label + "\n[predicate symbol]";
		}

		protected void addPredicateSymbols(Graph g)
		{
			var rigidRelations = problem.GetRigidRelations();
			foreach (var item in problem.GetIDManager().GetPredicatesMapping().getAllPredicateSymbolsID())
			{
				string predicateName = problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(item);
				var paramsCount = problem.GetIDManager().GetPredicatesMapping().GetNumberOfParameters(predicateName);
				if (rigidRelations.Any(r => r.GetPrefixID() == item))// &&	paramsCount <= 1)
					continue;	//rigid unary predicates are treated as types. They are not needed here.

				var label = getPredicateHeadID(item);
				var node = g.AddNode(label);
				formatAsPredicateSymbolNode(node);
			}
		}

		protected void addInitialPredicates(Graph g)
		{
			PDDLStateDefault state = (PDDLStateDefault)problem.GetInitialState();
			foreach (var item in state.GetPredicates())
			{
				var arity = item.GetParamCount();
				var constantIDs = Enumerable.Range(0, arity).Select(i => item.GetParam(i)).ToList();
				var constantNames = constantIDs.Select(ID => problem.GetIDManager().GetConstantsMapping().GetStringForConstID(ID)).ToList();
				var label = problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(item.GetPrefixID()) + "(" +
					string.Join(", ", constantNames) + ")\n[initial predicate]";
				var node = g.AddNode(label);
				node.Attr.FillColor = Color.Green;

				foreach (var constID in constantIDs)
				{
					var constNodeLabel = getConstLabel(constID, problem.GetIDManager().GetConstantsMapping());
					var edge = g.AddEdge(constNodeLabel, label);
				}

				g.AddEdge(label, getPredicateHeadID(item));
			}
		}

		protected void addGoalPredicates(Graph g)
		{
			foreach (var item in problem.GetGoalConditions().getAllPredicates())
			{
				var arity = item.GetParamCount();
				var constantIDs = Enumerable.Range(0, arity).Select(i => item.GetParam(i)).ToList();
				var constantNames = constantIDs.Select(ID => problem.GetIDManager().GetConstantsMapping().GetStringForConstID(ID)).ToList();
				var label = problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(item.GetPrefixID()) + "(" +
					string.Join(", ", constantNames) + ")\n[goal predicate]";
				var node = g.AddNode(label);
				formatAsGoalPredicateNode(node);

				foreach (var constID in constantIDs)
				{
					var constNodeLabel = getConstLabel(constID, problem.GetIDManager().GetConstantsMapping());
					var edge = g.AddEdge(constNodeLabel, label);
				}

				g.AddEdge(label, getPredicateHeadID(item));
			}
		}

		protected void formatAsGoalPredicateNode(Node n)
		{
			n.Attr.FillColor = Color.Gold;
			//n.Attr.Shape = Shape.Diamond;
		}

		protected void formatAsPredicateSymbolNode(Node n)
		{
			n.Attr.FillColor = Color.Pink;
			n.Attr.Shape = Shape.Diamond;
		}

		protected void formatAsTypeNode(Node n)
		{
			n.Attr.FillColor = Color.LightBlue;
			n.Attr.Shape = Shape.Box;
		}

		protected void formatAsTypeEdge(Edge e)
		{
			e.Attr.Color = Color.Blue;
		}

		protected void addRigidRelations(Graph g)
		{
			var rigidRelations = problem.GetRigidRelations();
			var predicateNameByID = new Func<int, string>(ID => problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(ID));
			var constantNameByID = new Func<int, string>(ID => problem.GetIDManager().GetConstantsMapping().GetStringForConstID(ID));

			foreach (var item in rigidRelations)
			{
				if (item.GetParamCount() == 1)  //unary rigid relations are treated as types
				{
					string typeLabel = predicateNameByID(item.GetPrefixID()) + "\n[rigid (type)]";
					string constLabel = getConstLabel(item.GetParam(0), problem.GetIDManager().GetConstantsMapping());
					Node n = g.AddNode(typeLabel);
					formatAsTypeNode(n);
					Edge e = g.AddEdge(constLabel, typeLabel);
					formatAsTypeEdge(e);
				}
				if (item.GetParamCount() == 2)
				{
					string const1Label = getConstLabel(item.GetParam(0), problem.GetIDManager().GetConstantsMapping());
					string const2Label = getConstLabel(item.GetParam(1), problem.GetIDManager().GetConstantsMapping());
					string relationLabel = predicateNameByID(item.GetPrefixID()) + "\n[rigid]";
					g.AddEdge(const1Label, relationLabel, const2Label);
				}
			}

		}

		protected string getConstLabel(int constID, PDDLConstantsMapping mapping)
		{
			return mapping.GetStringForConstID(constID) + "\n[const]";
		}

		protected string getTypeLabel(int typeID, PDDLTypesMapping mapping)
		{
			return mapping.getTypeNameByID(typeID) + "\n[type]";
		}

		public override Graph toMSAGLGraph()
		{
			Graph g = new Graph("Constant-predicate-type graph");
			bool useTyping = addTypes(g);
			addConstants(g, useTyping);
			addRigidRelations(g);
			addPredicateSymbols(g);
			addInitialPredicates(g);
			addGoalPredicates(g);
			return g;
		}
	}

    public class GraphEdge
    {
        //both outsideCondition and outside effect are in a form of two arrays - int he first are the variables and in the second are appropriate values. 
        //These are stored in SASEffect class in the 
        //atributes conditionVariables and conditionValues. (The SASEffect.effectVariable and SASEffect.effecValue are NOT used here, and shouldn't be accessed)
        public SASOperatorEffect outsideCondition, outsideEffect;
        public int from, to;
        public bool isRSE_Invertible, isInvertibilityComputed;
        public IOperator op;
    }
}
