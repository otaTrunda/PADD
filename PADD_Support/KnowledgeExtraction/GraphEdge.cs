using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support.KnowledgeExtraction
{
	public class GraphEdge
	{
		//both outsideCondition and outside effect are in a form of two arrays - int he first are the variables and in the second are appropriate values. 
		//These are stored in SASEffect class in the 
		//atributes conditionVariables and conditionValues. (The SASEffect.effectVariable and SASEffect.effecValue are NOT used here, and shouldn't be accessed)
		public PAD.Planner.SAS.IEffect outsideCondition, outsideEffect;
		public int from, to;
		public bool isRSE_Invertible, isInvertibilityComputed;
		public PAD.Planner.SAS.IOperator op;
	}
}
