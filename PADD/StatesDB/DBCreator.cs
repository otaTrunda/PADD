using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PADD.DomainDependentSolvers;

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
			foreach (var state in states)
			{
				samples++;
				if (samples > numberOfSamples || watch.Elapsed > maxTime)
					break;
				problem.SetInitialState(state);
				domainSpecificSolver.SetProblem(problem);
				int goalDistance = (int)Math.Floor(domainSpecificSolver.Search(quiet: true));
				var stateString = state.ToString();
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

}
