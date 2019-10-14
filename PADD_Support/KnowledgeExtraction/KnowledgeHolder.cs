using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support.KnowledgeExtraction
{

	public class KnowledgeHolder
	{
		public CausualGraph CG;
		public List<DomainTransitionGraph> DTGs;
		public PredicateConstantGraph predConstGraph;
		public HashSet<int> RSE_InvertibleVariables;

		public static KnowledgeHolder compute(PAD.Planner.SAS.Problem problem)
		{
			KnowledgeHolder result = new KnowledgeHolder();
			result.CG = KnowledgeExtractionGraphs.computeCausualGraph(problem);
			result.DTGs = new List<DomainTransitionGraph>();
			result.RSE_InvertibleVariables = new HashSet<int>();
			for (int i = 0; i < problem.Variables.Count; i++)
			{
				result.DTGs.Add(KnowledgeExtractionGraphs.computeDTG(problem, i));
				if (result.DTGs[result.DTGs.Count - 1].isRSE_Invertible)
					result.RSE_InvertibleVariables.Add(i);
			}
			return result;
		}

		public static KnowledgeHolder create(PAD.Planner.PDDL.Problem problem)
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

}
