using System;
using System.Linq;
using Microsoft.Msagl.Drawing;
using NeuralNetSpecificUtils.Graphs;
using PAD.Planner.PDDL;

namespace PADD_Support.KnowledgeExtraction
{
	public class PredicateConstantGraph : GraphVisualizable
	{
		PAD.Planner.PDDL.Problem problem;

		public PredicateConstantGraph(PAD.Planner.PDDL.Problem p)
		{
			this.problem = p;
		}

		protected bool addTypes(Graph g)
		{
			var typesManager = problem.IDManager.Types;
			var groundingManager = problem.EvaluationManager.GroundingManager;

			if (typesManager.GetUsedIDs().Count() <= 1)
				return false;
			foreach (var typeID in typesManager.GetUsedIDs())
			{
				var node = g.AddNode(getTypeLabel(typeID));
				formatAsTypeNode(node);
			}

			foreach (var parentTypeID in typesManager.GetUsedIDs())
			{
				var childrenTypes = groundingManager.GetChildrenTypes(parentTypeID);
				foreach (var childrenType in childrenTypes)
				{
					var edge = g.AddEdge(getTypeLabel(childrenType), "subtype", getTypeLabel(parentTypeID));
					formatAsTypeEdge(edge);

				}
			}
			return true;
		}

		protected void addConstants(Graph g, bool useTyping)
		{
			var idManager = problem.IDManager;
			var groundingManager = problem.EvaluationManager.GroundingManager;

			foreach (var item in idManager.Constants.GetUsedIDs())
			{
				var node = g.AddNode(getConstLabel(item));
				node.Attr.FillColor = Color.Red;
				if (useTyping)
				{
					var edge = g.AddEdge(node.Id, "hasType", getTypeLabel(groundingManager.GetTypesForConstant(item).First()));
					formatAsTypeEdge(edge);
				}
			}
		}

		protected string getPredicateHeadID(IAtom atom)
		{
			return getPredicateHeadID(atom.GetNameID());
		}

		protected string getPredicateHeadID(int predicateNameID)
		{
			var label = problem.IDManager.Predicates.GetNameFromID(predicateNameID);
			return label + "\n[predicate symbol]";
		}

		protected void addPredicateSymbols(Graph g)
		{
			var rigidRelations = problem.RigidRelations;
			foreach (var predicateID in problem.IDManager.Predicates.GetUsedIDs())
			{
				string predicateName = problem.IDManager.Predicates.GetNameFromID(predicateID);
				var paramsCount = problem.IDManager.Predicates.GetNumberOfArgumentsFromID(predicateID);
				if (rigidRelations.Any(r => r.GetNameID() == predicateID) && paramsCount <= 1)
					continue;   //rigid unary predicates are treated as types. They are not needed here.

				var label = getPredicateHeadID(predicateID);
				var node = g.AddNode(label);
				formatAsPredicateSymbolNode(node);
			}
		}

		protected void addInitialPredicates(Graph g)
		{
			var idManager = problem.IDManager;
			var state = problem.InitialState;

			foreach (var item in state.GetPredicates().Concat(problem.RigidRelations.Where(x => x.GetArity() > 1)))
			{
				var arity = item.GetArity();
				var constantIDs = Enumerable.Range(0, arity).Select(i => item.GetGroundedTerm(i));
				var constantNames = constantIDs.Select(ID => problem.IDManager.Constants.GetNameFromID(ID));
				var label = problem.IDManager.Predicates.GetNameFromID(item.GetNameID()) + "(" +
					string.Join(", ", constantNames) + ")\n[initial predicate]";
				var node = g.AddNode(label);
				node.Attr.FillColor = Color.Green;

				foreach (var constID in constantIDs)
				{
					var constNodeLabel = getConstLabel(constID);
					var edge = g.AddEdge(constNodeLabel, label);
				}

				g.AddEdge(label, getPredicateHeadID(item));
			}
		}

		protected void addGoalPredicates(Graph g)
		{
			var idManager = problem.IDManager;

			foreach (var item in problem.GoalConditions.GetUsedPredicates())
			{
				var arity = item.GetArity();
				var constantIDs = Enumerable.Range(0, arity).Select(i => item.GetGroundedTerm(i));
				var constantNames = constantIDs.Select(ID => idManager.Constants.GetNameFromID(ID));
				var label = idManager.Predicates.GetNameFromID(item.GetNameID()) + "(" +
					string.Join(", ", constantNames) + ")\n[goal predicate]";
				var node = g.AddNode(label);
				formatAsGoalPredicateNode(node);

				foreach (var constID in constantIDs)
				{
					var constNodeLabel = getConstLabel(constID);
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
			var idManager = problem.IDManager;
			var rigidRelations = problem.RigidRelations;
			var predicateNameByID = new Func<int, string>(ID => idManager.Predicates.GetNameFromID(ID));
			var constantNameByID = new Func<int, string>(ID => idManager.Constants.GetNameFromID(ID));

			foreach (var atom in rigidRelations)
			{
				if (atom.GetArity() == 1)  //unary rigid relations are treated as types
				{
					string typeLabel = predicateNameByID(atom.GetNameID()) + "\n[rigid (type)]";

					string constLabel = getConstLabel(atom.GetGroundedTerm(0));
					Node n = g.AddNode(typeLabel);
					formatAsTypeNode(n);
					Edge e = g.AddEdge(constLabel, typeLabel);
					formatAsTypeEdge(e);
				}
				continue;
				if (atom.GetArity() == 2)
				{
					string const1Label = getConstLabel(atom.GetGroundedTerm(0));
					string const2Label = getConstLabel(atom.GetGroundedTerm(1));
					string relationLabel = predicateNameByID(atom.GetNameID()) + "\n[rigid]";
					g.AddEdge(const1Label, relationLabel, const2Label);
				}
			}
		}

		protected string getConstLabel(int constID)
		{
			return problem.IDManager.Constants.GetNameFromID(constID) + "\n[const]";
		}

		protected string getTypeLabel(int typeID)
		{
			return problem.IDManager.Types.GetNameFromID(typeID) + "\n[type]";
		}

		public MyLabeledGraph ToMyGraph()
		{
			MyLabeledGraph res = new MyLabeledGraph();

			throw new Exception();




			return res;
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

}
