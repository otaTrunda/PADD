using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSP;

namespace PADD.DomainDependentSolvers.VisitAll
{
	class VisitAllState
	{
		public bool[] visited;
		public int position;
		public VisitAllDomain domain;

		public VisitAllState(SASState state, VisitAllDomain domain)
		{
			var values = state.GetAllValues();
			position = values[domain.positionVariable];
			visited = new bool[domain.nodes.Count];
			for (int i = 0; i < values.Length; i++)
			{
				if (i == domain.positionVariable)
					continue;
				if (values[i] == 0)
					visited[domain.nodeIDByVariableNo[i]] = true;
			}
			visited[domain.startPosition] = true;
			this.domain = domain;
		}

		public (TSPInput input, int position) toTSP()
		{
			TSPInput i = TSPInput.create((point1, point2) =>
			{
				var node1 = domain.nodes[domain.nodeIDByCoordinates[(int)point1.x][(int)point1.y]];
				var node2 = domain.nodes[domain.nodeIDByCoordinates[(int)point2.x][(int)point2.y]];
				return Math.Abs(node1.gridCoordX - node2.gridCoordX) + Math.Abs(node1.gridCoordY - node2.gridCoordY);
			});
			int realPosition = 0;
			foreach (var item in this.domain.nodes)
			{
				if (!visited[item.ID] || position == item.ID)
				{
					var point = TSPPoint.create(item.gridCoordX, item.gridCoordY);
					i.addPoint(point);
					if (position == item.ID)
					{
						realPosition = point.ID;
					}
				}
			}
			return (i, realPosition);
		}
	}
}
