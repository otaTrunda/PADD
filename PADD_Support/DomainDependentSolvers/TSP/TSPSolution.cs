using PADD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    struct TPSCouple
    {
        public int first, second;

        public TPSCouple(int first, int second)
        {
            this.first = first;
            this.second = second;
        }

        public int getTheOther(int value)
        {
            if (first == value)
                return second;
            else return first;
        }

        public void set(int value)
        {
            if (first == value)
                second = value;
            else first = value;
        }
    }

    public class TSPSolution
    {
        private bool isDistanceComputed = false;
        private double _totalDistance;
        
        private Dictionary<int, TPSCouple> succesors; //successors in the path. position of the neibhours indexed by the position of the node
        public TSPInput inp
        {
            get;
            private set;
        }
        public double totalDistance
        {
            get
            {
                if (!isDistanceComputed)
					_totalDistance = computeDistance();
                return _totalDistance;
            }
            private set
            {
                _totalDistance = value;
            }
        }
        public bool isValid
        {
            get;
            private set;
        }
        public List<Edge> edges;

        public TSPSolution(TSPInput inp)
        {
            this.inp = inp;
            this.succesors = new Dictionary<int, TPSCouple>();
            this.edges = new List<Edge>();
        }
   
        public virtual double computeDistance()
        {
            if (isDistanceComputed)
                return _totalDistance;
            _totalDistance = 0;
			if (inp.nodesCount <= 1)
				return 0;

            for (int i = 0, j = 0; i < inp.nodesCount; i++, j = getSuccessor(j))
            {
                _totalDistance += inp.getDistance(j, getSuccessor(j));
            }
            isDistanceComputed = true;
            return totalDistance;
        }
        public int getSuccessor(int node, int notWantedSuccesor)
        {
            return succesors[node].getTheOther(notWantedSuccesor);
        }
        public int getSuccessor(int node)
        {
            return succesors[node].first;
        }
        public void setSuccessor(int node, int successor)
        {
            isDistanceComputed = false;
            if (!succesors.ContainsKey(node))
                succesors.Add(node, new TPSCouple(successor, successor));
            succesors[node].set(successor);
            edges.Add(new Edge(node, successor, this.inp, inp.getDistance(node, successor)));
        }
        public bool validate()
        {
            isValid = false;
            HashSet<int> visited = new HashSet<int>();
            int node = 0;
            while (!visited.Contains(node))
            {
                visited.Add(node);
                if (!succesors.ContainsKey(node))
                    return false;
                node = getSuccessor(node);
            }
            if (visited.Count < inp.nodesCount)
                return false;

            isValid = true;
            return true;
        }

        internal static TSPSolution fromString(string result, TSPInput inp)
        {
            string[] points = result.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (points.Length != inp.nodesCount)
            {
                System.Windows.Forms.MessageBox.Show("Spatna delka reseni.\nReseni ma mit delku " + inp.nodesCount + ". Vase reseni ma delku " + points.Length);
                return null;
            }
            TSPSolution sol = new TSPSolution(inp);
            for (int i = 0; i < points.Length - 1; i++)
            {
                sol.setSuccessor(int.Parse(points[i]) - 1, int.Parse(points[i + 1]) - 1);
            }
            sol.setSuccessor(int.Parse(points[points.Length - 1]) - 1, int.Parse(points[0]) - 1);
            return sol;
        }
    }

	public class TSPSolutionPath : TSPSolution
	{
		public int startNode, endNode;
		public TSPSolutionPath(TSPInput inp, int startNode, int endNode) : base(inp)
		{
			this.startNode = startNode;
			this.endNode = endNode;
		}

		public override double computeDistance()
		{
			return base.computeDistance() - inp.getDistance(startNode, endNode);
		}
	}
}
