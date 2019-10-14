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
using PAD.Planner.SAS;
using PAD.Planner.Heuristics;
using PAD.Planner.Search;

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
			enumerator.problem = new Problem(problemFile, false);
			var states = enumerator.enumerateStates();
			var problem = new Problem(problemFile, false);
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
			enumerator.problem = new Problem(problemFile, false);
			var states = enumerator.enumerateStates();
			var problem = new Problem(problemFile, false);
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
		public Problem problem;

		public abstract IEnumerable<IState> enumerateStates();

		public StatesEnumerator(Problem problem)
		{
			this.problem = problem;
		}
	}

	public class RandomWalkStateSpaceEnumerator : StatesEnumerator
	{
		public IState initialState;

		public override IEnumerable<IState> enumerateStates()
		{
			IState currentState = initialState;

			while(true)
			{
				currentState = (IState)problem.GetRandomSuccessor(currentState).GetSuccessorState();
				yield return currentState;
			}
		}

		public RandomWalkStateSpaceEnumerator(Problem problem)
			:base(problem)
		{
			this.initialState = (IState)problem.GetInitialState();
		}

	}

	public class RandomWalksFromGoalPathStateSpaceEnumerator : StatesEnumerator
	{
		List<RandomWalkStateSpaceEnumerator> StateSpaceEnumeratos;
		public List<IState> goalPath;

		HeuristicSearch goalPathFinder;
		DomainDependentSolver domainSolver;

		public RandomWalksFromGoalPathStateSpaceEnumerator(Problem problem, DomainDependentSolver domainDependentSolver) 
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

		List<IState> findGoalPath()
		{
			if (domainSolver.canFindPlans)
			{
				domainSolver.Search();
				var plan = domainSolver.getPDDLPlan();

				var sasPlan = plan.Select(s => s.Replace("(", "").Replace(")", "")).Select(s => (PAD.Planner.IOperator)problem.Operators.Where(op => op.GetName() == s).Single());
				SolutionPlan p = new SolutionPlan(problem.GetInitialState(), sasPlan);
				return p.GetStatesSequence().Select(state => (IState)state).ToList();
			}

			goalPathFinder.Start();
			return goalPathFinder.GetSolutionPlan().GetStatesSequence().Select(state => (IState)state).ToList();
		}

		public override IEnumerable<IState> enumerateStates()
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

		public override string GetDescription()
		{
			return "Solver used as heuristic";
		}

		protected override double GetValueImpl(PAD.Planner.IState state)
		{
			var problem = solver.sasProblem;
			problem.SetInitialState(state);
			solver.SetProblem(problem);
			return solver.Search(quiet: true);
		}

		public HeuristicWrapper(DomainDependentSolver solver) : base(null)
		{
			this.solver = solver;
		}
	}

	public static class Helper
	{
		static Dictionary<string, Problem> loadedProblems = new Dictionary<string, Problem>();

		private static Problem getProblem(string description)
		{
			if (!loadedProblems.ContainsKey(description))
			{
				string[] parts = description.Split('_');
				Problem p = new Problem(@"C:\Users\Trunda_Otakar\Documents\Visual Studio 2017\Projects\PADD - NEW\PADD\PADD\bin\tests\benchmarksSAS_ALL_withoutAxioms\" + parts[0] + "\\" + parts[1], false);
				loadedProblems.Add(description, p);
			}
			return loadedProblems[description];
		}

		public static (IState state, Problem problem) ReconstructState(string stateInfo)
		{
			string[] parts1 = stateInfo.Split('_');
			Problem p1 = getProblem(parts1[0] + "_" + parts1[1]);
			IState s1 = State.Parse(parts1[2]);
			return (s1, p1);
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
		Problem problem;
		IState originalInitialState;

		public IEnumerable<(Problem planningProblem, IState state, double trueGoalDistance)> enumerateStatesWithDistances(int repeats = 10)
		{
			for (int i = 0; i < repeats; i++)
			{
				Console.WriteLine("Heuristic multiplier: " + h.multiplier);
				problem.SetInitialState(originalInitialState);
				h.perfectDistances.Clear();
				astar = new AStarSearch(this.problem, h, null);
				astar.TimeLimitOfSearch = TimeSpan.FromHours(5);
				astar.Start();

				foreach (var item in h.perfectDistances)
				{
					yield return (problem, (IState)item.Item1, item.Item2);
				}

				Console.WriteLine();
			}
		}

		/// <summary>
		/// Returns minimum and maximum length of plans for all problems from the same domain as given problem.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		protected (int min, int max) getPlanLengthMinMax(Problem p)
		{
			var SASFiles = Directory.EnumerateFiles(Path.GetDirectoryName(p.GetInputFilePath())).Where(f => Path.GetExtension(f) == ".sas").ToList();
			return SASFiles.Select(f => getPlanLength(f)).ToList().GetMinMax();
		}

		protected int getPlanLength(Problem p)
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

		public AStarSearchEnumerator(Problem p, DomainType domain, double noisePercentage)
		{
			var planLengthMinMax = getPlanLengthMinMax(p);
			Utils.Transformations.Mapping m = new Utils.Transformations.LinearMapping(planLengthMinMax.min, planLengthMinMax.max, 1, 2);

			problem = p;
			h = new NoisyPerfectHeuristic(p, domain, noisePercentage);
			h.multiplier = m.getVal(getPlanLength(p));
			originalInitialState = p.InitialState;
		}

		public static IEnumerable<(Problem planningProblem, IState state, double trueGoalDistance)> enumerateStatesWithDistances(List<Problem> problems, DomainType domain, int repeats = 10)
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

		public static void storeStatesAsTSV(string tsvFile, List<Problem> problems, DomainType domain, int repeats = 10)
		{
			var states = enumerateStatesWithDistances(problems, domain, repeats);
			File.WriteAllLines(tsvFile, states.Select(x => x.state.GetInfoString(x.planningProblem) + "\t" + x.trueGoalDistance));
		}
	}
}
