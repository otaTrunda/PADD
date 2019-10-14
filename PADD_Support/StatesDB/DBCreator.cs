using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PADD.DomainDependentSolvers;
using NeuralNetSpecificUtils.Graphs;
using Utils.ExtensionMethods;
using Utils.MachineLearning;
using NeuralNetSpecificUtils.GraphFeatureGeneration;
using PADD_Support;

namespace PADD.StatesDB
{
	/// <summary>
	/// For given problem file, it enumerates states from its state-space and for each of them it computes the real-goal-distance (using given solver) and stores it in a trie that is then saved to disk.
	/// </summary>
	public class DBCreator
	{
		public Trie<int> DB;
		string DBFolderName = "_trieDBs";
		StatesEnumerator enumerator;

		public string getDBFilePath(string problemFilePath)
		{
			string FolderPath = Path.Combine(Path.GetDirectoryName(problemFilePath), DBFolderName);
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
			string problemFileName = Path.GetFileName(problemFilePath);
			string DBFileName = Path.ChangeExtension(Path.Combine(FolderPath, problemFileName), ".txt");
			return DBFileName;
		}

		public IEnumerable<(string key, int val)> createSamples(string problemFile, DomainDependentSolver domainSpecificSolver, long numberOfSamples, TimeSpan maxTime, bool storeDB = true)
		{
			System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			int samples = 0;
			enumerator.problem = SASProblem.CreateFromFile(problemFile);
			var states = enumerator.enumerateStates();
			var problem = SASProblem.CreateFromFile(problemFile);
			HashSet<string> alreadyGenerated = new HashSet<string>();
			int hashSetHits = 0;

			foreach (var state in states)
			{
				if (samples > numberOfSamples || watch.Elapsed > maxTime || hashSetHits > alreadyGenerated.Count)
					break;

				string stateString = state.ToString();
				if (alreadyGenerated.Contains(stateString))
				{
					hashSetHits++;
					continue;
				}
				alreadyGenerated.Add(stateString);
				samples++;
				problem.SetInitialState(state);
				domainSpecificSolver.SetProblem(problem);
				int goalDistance = (int)Math.Floor(domainSpecificSolver.Search(quiet: true));
				yield return (stateString.Substring(0, stateString.Length - 2), goalDistance); //skipes two last two characters of the string. They are always the same.
			}
		}

		public Trie<int> createDB(string problemFile, DomainDependentSolver domainSpecificSolver, long numberOfSamples, TimeSpan maxTime, bool storeDB = true)
		{
			System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			DB = new Trie<int>();
			int samples = 0;
			enumerator.problem = SASProblem.CreateFromFile(problemFile);
			var states = enumerator.enumerateStates();
			var problem = SASProblem.CreateFromFile(problemFile);
			foreach (var state in states)
			{
				samples++;
				if (samples > numberOfSamples || watch.Elapsed > maxTime)
					break;
				problem.SetInitialState(state);
				domainSpecificSolver.SetProblem(problem);
				int goalDistance = (int)Math.Floor(domainSpecificSolver.Search(quiet: true));
				var stateString = state.ToString();
				DB.add(stateString.Substring(0, stateString.Length - 2), goalDistance);	//skipes two last two characters of the string. They are always the same.
			}
			if (storeDB)
				DB.store(getDBFilePath(problemFile));
			return DB;
		}

		public DBCreator(StatesEnumerator enumerator)
		{
			this.enumerator = enumerator;
		}
	}

	/// <summary>
	/// Can enumerate states of some problem by some strategy.
	/// </summary>
	public abstract class StatesEnumerator
	{
		public SASProblem problem;

		public abstract IEnumerable<SASState> enumerateStates();

		public StatesEnumerator(SASProblem problem)
		{
			this.problem = problem;
		}
	}

	public class RandomWalkStateSpaceEnumerator : StatesEnumerator
	{
		public SASState initialState;

		public override IEnumerable<SASState> enumerateStates()
		{
			SASState currentState = initialState;

			while(true)
			{
				currentState = (SASState)problem.GetRandomSuccessor(currentState).GetSuccessorState();
				yield return currentState;
			}
		}

		public RandomWalkStateSpaceEnumerator(SASProblem problem)
			:base(problem)
		{
			this.initialState = (SASState)problem.GetInitialState();
		}

	}

	public class RandomWalksFromGoalPathStateSpaceEnumerator : StatesEnumerator
	{
		List<RandomWalkStateSpaceEnumerator> StateSpaceEnumeratos;
		public List<SASState> goalPath;

		HeuristicSearchEngine goalPathFinder;
		DomainDependentSolver domainSolver;

		public RandomWalksFromGoalPathStateSpaceEnumerator(SASProblem problem, DomainDependentSolver domainDependentSolver) 
			: base(problem)
		{
			domainDependentSolver.SetProblem(problem);
			this.goalPathFinder = new HillClimbingSearch(problem, new HeuristicWrapper(domainDependentSolver));
			domainSolver = domainDependentSolver;
			goalPath = findGoalPath();
			StateSpaceEnumeratos = goalPath.Select(s =>
			{
				var enume = new RandomWalkStateSpaceEnumerator(problem);
				enume.initialState = s;
				return enume;
			}).ToList();
			
		}

		List<SASState> findGoalPath()
		{
			if (domainSolver.canFindPlans)
			{
				domainSolver.Search();
				var plan = domainSolver.getPDDLPlan();

				var sasPlan = plan.Select(s => s.Replace("(", "").Replace(")", "")).Select(s => (IOperator)problem.GetOperators().Where(op => op.GetName() == s).Single()).ToList();
				SolutionPlan p = new SolutionPlan(sasPlan);
				return p.getSequenceOfStates(problem.GetInitialState()).Select(state => (SASState)state).ToList();
			}

			var initialState = goalPathFinder.problem.GetInitialState();
			goalPathFinder.Search(quiet: true);
			return goalPathFinder.GetSolution().getSequenceOfStates(initialState).Select(state => (SASState)state).ToList();
		}

		public override IEnumerable<SASState> enumerateStates()
		{
			var enumerators = StateSpaceEnumeratos.Select(s => s.enumerateStates().GetEnumerator()).ToList();
			var hasNext = enumerators.Select(en => en.MoveNext()).ToList();
			int i = 0;
			while(hasNext.Any(t => t))
			{
				i = (i + 1) % enumerators.Count;
				if (hasNext[i])
				{
					yield return enumerators[i].Current;
					hasNext[i] = enumerators[i].MoveNext();
				}
			}
		}
	}

	public class HeuristicWrapper : Heuristic
	{
		DomainDependentSolver solver;

		public override string getDescription()
		{
			return "Solver used as heuristic";
		}

		protected override double evaluate(IState state)
		{
			var problem = solver.sasProblem;
			problem.SetInitialState(state);
			solver.SetProblem(problem);
			return solver.Search(quiet: true);
		}

		public HeuristicWrapper(DomainDependentSolver solver)
		{
			this.solver = solver;
		}
	}

	public static class Helper
	{
		static Dictionary<string, SASProblem> loadedProblems = new Dictionary<string, SASProblem>();

		private static SASProblem getProblem(string description)
		{
			if (!loadedProblems.ContainsKey(description))
			{
				string[] parts = description.Split('_');
				SASProblem p = SASProblem.CreateFromFile(@"C:\Users\Trunda_Otakar\Documents\Visual Studio 2017\Projects\PADD - NEW\PADD\PADD\bin\tests\benchmarksSAS_ALL_withoutAxioms\" + parts[0] + "\\" + parts[1]);
				loadedProblems.Add(description, p);
			}
			return loadedProblems[description];
		}

		public static SASState ReconstructState(string stateInfo)
		{
			string[] parts1 = stateInfo.Split('_');
			SASProblem p1 = getProblem(parts1[0] + "_" + parts1[1]);
			SASState s1 = SASState.parse(parts1[2], p1);
			return s1;
		}

		/// <summary>
		/// Takes a list of training samples (previously created from SAS-states), transforms them back to SAS-states and then back to training samples using updated procedure
		/// </summary>
		/// <param name="samples"></param>
		public static IEnumerable<TrainingSample> ReGenerateSamples(List<TrainingSample> samples, string storeGeneratorPath = "")
		{
			SubgraphsSignatures_FeaturesGenerator g = SubgraphsSignatures_FeaturesGenerator.load("trainedGeneratorNEW.bin");
			loadedProblems = new Dictionary<string, SASProblem>();

			string[] parts1 = samples.First().userData.Split('_');
			SASProblem p1 = getProblem(parts1[0] + "_" + parts1[1]);
			SASState s1 = SASState.parse(parts1[2], p1);
			p1.SetInitialState(s1);
			var sampleGraph = KnowledgeExtraction.computeObjectGraph(p1).toMSAGLGraph();
			var labelingFunction = NeuralNetSpecificUtils.UtilsMethods.getLabelingFunction(sampleGraph);
			var labeledGraphs = new List<MyLabeledGraph>();
			int i = 0;
			foreach (var item in samples)
			{
				i++;
				var t = item.userData;
				string[] parts = t.Split('_');
				SASProblem p = getProblem(parts[0] + "_" + parts[1]);
				SASState s = SASState.parse(parts[2], p);
				p.SetInitialState(s);
				var graph = KnowledgeExtraction.computeObjectGraph(p).toMSAGLGraph();
				labeledGraphs.Add(MyLabeledGraph.createFromMSAGLGraph(graph, labelingFunction.labelingFunc, labelingFunction.labelSize));
				if (i % 10000 == 0)
					Console.WriteLine("Completed " + i + " out of " + samples.Count + " .Time: " + DateTime.Now.ToString());
			}
			g = new SubgraphsSignatures_FeaturesGenerator(".");
			g.train(labeledGraphs, 4);
			if (storeGeneratorPath != "")
				g.save(storeGeneratorPath);
			g.save("trainedGeneratorNEW.bin");
			Console.WriteLine("Transformation started");
			foreach (var item in samples.Zip(labeledGraphs))
			{
				var res = new TrainingSample(g.getFeatures(item.Item2), item.Item1.targets);
				res.userData = item.Item1.userData;
				yield return res;
			}
		}
	}

	/// <summary>
	/// Runs A* on the given problem and stores all states that were added to the open list
	/// Uses a noisyPerfect heuristic during the search that should emulate trained NN. This should therefore be close to the set of states that NN would encounter during deployment.
	/// </summary>
	public class AStarSearchEnumerator
	{
		AStarSearch astar;
		NoisyPerfectHeuristic h;
		//Heuristic trueGoalDistance;
		SASProblem problem;
		IState originalInitialState;

		public IEnumerable<(SASState state, double trueGoalDistance)> enumerateStatesWithDistances(int repeats = 10)
		{
			for (int i = 0; i < repeats; i++)
			{
				Console.WriteLine("Heuristic multiplier: " + h.multiplier);
				problem.SetInitialState(originalInitialState);
				h.perfectDistances.Clear();
				astar = new AStarSearch(this.problem, h);
				astar.timeLimit = TimeSpan.FromHours(5);
				astar.Search(quiet: false);

				foreach (var item in h.perfectDistances)
				{
					yield return ((SASState)item.Item1, item.Item2);
				}

				Console.WriteLine();
			}
		}

		/// <summary>
		/// Returns minimum and maximum length of plans for all problems from the same domain as given problem.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		protected (int min, int max) getPlanLengthMinMax(SASProblem p)
		{
			var SASFiles = Directory.EnumerateFiles(Path.GetDirectoryName(p.GetInputFilePath())).Where(f => Path.GetExtension(f) == ".sas").ToList();
			return SASFiles.Select(f => getPlanLength(f)).ToList().GetMinMax();
		}

		protected int getPlanLength(SASProblem p)
		{
			var file = Path.Combine(Path.GetDirectoryName(p.GetInputFilePath()), "plans", Path.GetFileNameWithoutExtension(p.GetInputFilePath()) + ".txt");
			int linesCount = File.ReadAllLines(file).Count();
			return linesCount;
		}

		protected int getPlanLength(string filename)
		{
			var file = Path.Combine(Path.GetDirectoryName(filename), "plans", Path.GetFileNameWithoutExtension(filename) + ".txt");
			int linesCount = File.ReadAllLines(file).Count();
			return linesCount;
		}

		public AStarSearchEnumerator(SASProblem p, DomainType domain, double noisePercentage)
		{
			var planLengthMinMax = getPlanLengthMinMax(p);
			Utils.Transformations.Mapping m = new Utils.Transformations.LinearMapping(planLengthMinMax.min, planLengthMinMax.max, 1, 2);

			problem = p;
			h = new NoisyPerfectHeuristic(p, domain, noisePercentage);
			h.multiplier = m.getVal(getPlanLength(p));
			originalInitialState = p.GetInitialState();
		}

		public static IEnumerable<(SASState state, double trueGoalDistance)> enumerateStatesWithDistances(List<SASProblem> problems, DomainType domain, int repeats = 10)
		{
			foreach (var item in problems)
			{
				AStarSearchEnumerator s = new AStarSearchEnumerator(item, domain, noisePercentage: 1d/100);
				var res = s.enumerateStatesWithDistances(repeats);
				foreach (var resItem in res)
				{
					yield return resItem;
				}
			}
		}

		public static void storeStatesAsTSV(string tsvFile, List<SASProblem> problems, DomainType domain, int repeats = 10)
		{
			var states = enumerateStatesWithDistances(problems, domain, repeats);
			File.WriteAllLines(tsvFile, states.Select(x => x.state.GetInfoString() + "\t" + x.trueGoalDistance));
		}
	}
}
