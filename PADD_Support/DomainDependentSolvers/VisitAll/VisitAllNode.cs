using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.VisitAll
{
	public class VisitAllNode
	{
		private static int IDCounter = 0;
		public static void resetIDCounter()
		{
			IDCounter = 0;
		}

		public int ID;
		public string originalName;
		public int variableNumber;
		public int gridCoordX, gridCoordY;
		public List<VisitAllNode> successors;
		public VisitAllNode(string originalName)
		{
			this.ID = IDCounter++;
			this.originalName = originalName;
			var splitted = originalName.Split('-').Skip(1).ToList();
			gridCoordX = int.Parse(splitted[0].Substring(1));
			gridCoordY = int.Parse(splitted[1].Substring(1));
			this.successors = new List<VisitAllNode>();
		}
	}

}
