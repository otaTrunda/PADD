using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	class CycleDetection
	{
		public static List<List<int>> findElementaryCycles((Dictionary<int, List<int>> outEdges, Dictionary<int, List<int>> inEdges, HashSet<int> allNodes) graph)
		{
			var searchEngine = new ElementaryCyclesSearch(graph.outEdges, graph.allNodes);
			return searchEngine.getElementaryCycles();
		}

		public static void testElementaryCycles()
		{
			bool[,] adjMatrix = new bool[9, 9];

			adjMatrix[0,1] = true;
			adjMatrix[1,2] = true;
			adjMatrix[2,0] = true; adjMatrix[2,6] = true;
			adjMatrix[3,4] = true;
			adjMatrix[4,5] = true; adjMatrix[4,6] = true;
			adjMatrix[5,3] = true;
			adjMatrix[6,7] = true;
			adjMatrix[7,8] = true;
			adjMatrix[8,6] = true;
			adjMatrix[6,1] = true;

			List<(int, int)> edges = new List<(int, int)>();
			for (int i = 0; i < adjMatrix.GetLength(0); i++)
				for (int j = 0; j < adjMatrix.GetLength(1); j++)
				{
					if (adjMatrix[i, j])
						edges.Add((i, j));
				}

			var outEdges = edges.GroupBy(r => r.Item1).ToDictionary(t => t.Key, t => t.Select(r => r.Item2).ToList());
			var allnodes = new HashSet<int>(Enumerable.Range(0, adjMatrix.GetLength(0)));

			var cycles = new ElementaryCyclesSearch(outEdges, allnodes).getElementaryCycles();
		}
	}

	//Rest of the code is ported from java and heavilly adjusted.

	public class ElementaryCyclesSearch
	{
		/** List of cycles */
		private List<List<int>> cycles = null;

		/** Adjacency-list of graph */
		private Dictionary<int, List<int>> adjList = null;

		/** Graphnodes */
		private List<int> graphNodes = null;

		/** Blocked nodes, used by the algorithm of Johnson */
		private Dictionary<int, bool> blocked = null;

		/** B-Lists, used by the algorithm of Johnson */
		private Dictionary<int, List<int>> B = null;

		/** Stack for nodes, used by the algorithm of Johnson */
		private List<int> stack = null;

		/**
		 * Constructor.
		 *
		 * @param matrix adjacency-matrix of the graph
		 * @param graphNodes array of the graphnodes of the graph; this is used to
		 * build sets of the elementary cycles containing the objects of the original
		 * graph-representation
		 */
		public ElementaryCyclesSearch(Dictionary<int, List<int>> outEdges, HashSet<int> nodesIDs)
		{
			this.graphNodes = nodesIDs.OrderBy(r => r).ToList();
			this.adjList = outEdges;
		}

		/**
		 * Returns List::List::Object with the Lists of nodes of all elementary
		 * cycles in the graph.
		 *
		 * @return List::List::Object with the Lists of the elementary cycles.
		 */
		public List<List<int>> getElementaryCycles()
		{
			this.cycles = new List<List<int>>();
			this.blocked = graphNodes.ToDictionary(n => n, n => false);
			this.B = graphNodes.ToDictionary(n => n, n => new List<int>());
			this.stack = new List<int>();
			StronglyConnectedComponents sccs = new StronglyConnectedComponents(adjList);
			int s = 0;

			while (true)
			{
				SCCResult sccResult = sccs.getAdjacencyList(s, graphNodes);
				if (sccResult != null && sccResult.getAdjList() != null)
				{
					var scc = sccResult.getAdjList();
					s = sccResult.getLowestNodeId();

					foreach (var j in scc.Keys)
					{
						if ((scc[j] != null) && (scc[j].Count > 0))
						{
							blocked[j] = false;
							B[j] = new List<int>();
						}
					}

					findCycles(s, s, scc);
					s = StronglyConnectedComponents.getNextNode(s, graphNodes);
				}
				else
				{
					break;
				}
			}

			return cycles;
		}

		/**
		 * Calculates the cycles containing a given node in a strongly connected
		 * component. The method calls itself recursivly.
		 *
		 * @param v
		 * @param s
		 * @param adjList adjacency-list with the subgraph of the strongly
		 * connected component s is part of.
		 * @return true, if cycle found; false otherwise
		 */
		private bool findCycles(int v, int s, Dictionary<int, List<int>> adjList)
		{
			bool f = false;
			this.stack.Add(v);
			this.blocked[v] = true;

			for (int i = 0; i < adjList[v].Count; i++)
			{
				int w = adjList[v][i];
				// found cycle
				if (w == s)
				{
					List<int> cycle = new List<int>();
					for (int j = 0; j < this.stack.Count; j++)
					{
						int index = stack[j];
						//cycle.Add(graphNodes[index]);
						cycle.Add(index);
					}
					this.cycles.Add(cycle);
					f = true;
				}
				else if (!this.blocked[w])
				{
					if (this.findCycles(w, s, adjList))
					{
						f = true;
					}
				}
			}

			if (f)
			{
				this.unblock(v);
			}
			else
			{
				for (int i = 0; i < adjList[v].Count; i++)
				{
					int w = adjList[v][i];
					if (!B[w].Contains(v))
					{
						B[w].Add(v);
					}
				}
			}

			this.stack.Remove(v);
			return f;
		}

		/**
		 * Unblocks recursivly all blocked nodes, starting with a given node.
		 *
		 * @param node node to unblock
		 */
		private void unblock(int node)
		{
			this.blocked[node] = false;
			List<int> Bnode = this.B[node];
			while (Bnode.Count > 0)
			{
				int w = Bnode[0];
				Bnode.RemoveAt(0);
				if (this.blocked[w])
				{
					this.unblock(w);
				}
			}
		}
	}

	public class SCCResult
	{
		private HashSet<int> nodeIDsOfSCC = null;
		private Dictionary<int, List<int>> adjList = null;
		private int lowestNodeId = -1;

		public SCCResult(Dictionary<int, List<int>> adjList, int lowestNodeId, List<int> allnodes)
		{
			this.adjList = adjList;
			this.lowestNodeId = lowestNodeId;
			this.nodeIDsOfSCC = new HashSet<int>();
			if (this.adjList != null)
			{
				foreach (var i in allnodes.Where(n => n >= lowestNodeId))
				{
					if (this.adjList.ContainsKey(i) && this.adjList[i].Count() > 0)
					{
						this.nodeIDsOfSCC.Add(i);
					}
				}
			}
		}

		public Dictionary<int, List<int>> getAdjList()
		{
			return adjList;
		}

		public int getLowestNodeId()
		{
			return lowestNodeId;
		}
	}

	public class StronglyConnectedComponents
	{
		/** Adjacency-list of original graph */
		private Dictionary<int, List<int>> adjListOriginal = null;

		/** Adjacency-list of currently viewed subgraph */
		private Dictionary<int, List<int>> adjList = null;

		/** Helpattribute for finding scc's */
		private Dictionary<int, bool> visited = null;

		/** Helpattribute for finding scc's */
		private List<int> stack = null;

		/** Helpattribute for finding scc's */
		private Dictionary<int, int> lowlink = null;

		/** Helpattribute for finding scc's */
		private Dictionary<int, int> number = null;

		/** Helpattribute for finding scc's */
		private int sccCounter = 0;

		/** Helpattribute for finding scc's */
		private List<List<int>> currentSCCs = null;

		private List<int> allNodes;

		/**
		 * Constructor.
		 *
		 * @param adjList adjacency-list of the graph
		 */
		public StronglyConnectedComponents(Dictionary<int, List<int>> adjList)
		{
			this.adjListOriginal = adjList;
		}

		/**
		 * This method returns the adjacency-structure of the strong connected
		 * component with the least vertex in a subgraph of the original graph
		 * induced by the nodes {s, s + 1, ..., n}, where s is a given node. Note
		 * that trivial strong connected components with just one node will not
		 * be returned.
		 *
		 * @param node node s
		 * @return SCCResult with adjacency-structure of the strong
		 * connected component; null, if no such component exists
		 */
		public SCCResult getAdjacencyList(int node, List<int> allNodes)
		{
			this.allNodes = allNodes;
			this.visited = allNodes.ToDictionary(r => r, r => false);
			this.lowlink = allNodes.ToDictionary(r => r, r => 0);
			this.number = allNodes.ToDictionary(r => r, r => 0);
			this.stack = new List<int>();
			this.currentSCCs = new List<List<int>>();

			this.makeAdjListSubgraph(node);

			foreach (var i in adjListOriginal.Keys.Where(k => k >= node))
			{
				if (!this.visited[i])
				{
					this.getStrongConnectedComponents(i);
					List<int> nodes = this.getLowestIdComponent();
					if (nodes != null && !nodes.Contains(node) && !nodes.Contains(getNextNode(node, allNodes)))
					{
						return this.getAdjacencyList(node + 1, allNodes);
					}
					else
					{
						Dictionary<int, List<int>> adjacencyList = this.getAdjList(nodes);
						if (adjacencyList != null)
						{
							foreach (var j in adjListOriginal.Keys)
							{
								if (adjacencyList.ContainsKey(j) && adjacencyList[j].Count > 0)
								{
									return new SCCResult(adjacencyList, j, allNodes);
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns node that is just after the given one in <paramref name="orderedList"/>. If the given node is the last one, it will return this.allNodes.Last() + 1
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static int getNextNode(int node, List<int> orderedList)
		{
			for (int i = 0; i < orderedList.Count - 1; i++)
			{
				if (orderedList[i] == node)
					return orderedList[i + 1];
			}
			return orderedList[orderedList.Count - 1] + 1;
		}	

		/**
		 * Builds the adjacency-list for a subgraph containing just nodes
		 * >= a given index.
		 *
		 * @param node Node with lowest index in the subgraph
		 */
		private void makeAdjListSubgraph(int node)
		{
			this.adjList = adjListOriginal.Keys.ToDictionary(k => k, k => new List<int>());

			foreach (var i in adjListOriginal.Keys)
			{
				List<int> successors = new List<int>();

				foreach (var j in adjListOriginal[i])
				{
					if (j >= node)
					{
						successors.Add(j);
					}
				}
				if (successors.Count > 0)
				{
					this.adjList[i] = successors;
				}
			}
		}

		/**
		 * Calculates the strong connected component out of a set of scc's, that
		 * contains the node with the lowest index.
		 *
		 * @return List<int>::Integer of the scc containing the lowest nodenumber
		 */
		private List<int> getLowestIdComponent()
		{
			int min = int.MaxValue;
			List<int> currScc = null;

			for (int i = 0; i < this.currentSCCs.Count; i++)
			{
				List<int> scc = currentSCCs[i];
				for (int j = 0; j < scc.Count; j++)
				{
					int node = scc[j];
					if (node < min)
					{
						currScc = scc;
						min = node;
					}
				}
			}

			return currScc;
		}

		/**
		 * @return List<int>[]::Integer representing the adjacency-structure of the
		 * strong connected component with least vertex in the currently viewed
		 * subgraph
		 */
		private Dictionary<int, List<int>> getAdjList(List<int> nodes)
		{
			Dictionary<int, List<int>> lowestIdAdjacencyList = null;

			if (nodes != null)
			{
				lowestIdAdjacencyList = nodes.ToDictionary(k => k, k => new List<int>());
				foreach (var node in nodes)
				{
					for (int j = 0; j < this.adjList[node].Count; j++)
					{
						int succ = this.adjList[node][j];
						if (nodes.Contains(succ))
						{
							lowestIdAdjacencyList[node].Add(succ);
						}
					}
				}
			}

			return lowestIdAdjacencyList;
		}

		/**
		 * Searchs for strong connected components reachable from a given node.
		 *
		 * @param root node to start from.
		 */
		private void getStrongConnectedComponents(int root)
		{
			this.sccCounter++;
			this.lowlink[root] = this.sccCounter;
			this.number[root] = this.sccCounter;
			this.visited[root] = true;
			this.stack.Add(root);

			if (adjList.ContainsKey(root))
				for (int i = 0; i < this.adjList[root].Count; i++)
				{
					int w = this.adjList[root][i];
					if (!this.visited[w])
					{
						this.getStrongConnectedComponents(w);
						this.lowlink[root] = Math.Min(lowlink[root], lowlink[w]);
					}
					else if (this.number[w] < this.number[root])
					{
						if (this.stack.Contains(w))
						{
							lowlink[root] = Math.Min(this.lowlink[root], this.number[w]);
						}
					}
				}

			// found scc
			if ((lowlink[root] == number[root]) && (stack.Count > 0))
			{
				int next = -1;
				List<int> scc = new List<int>();

				do
				{
					next = stack[stack.Count - 1];
					this.stack.RemoveAt(stack.Count - 1);
					scc.Add(next);
				} while (this.number[next] > this.number[root]);

				// simple scc's with just one node will not be added
				if (scc.Count > 1)
				{
					this.currentSCCs.Add(scc);
				}
			}
		}
	}
}

