using PADD;
using PADD.DomainDependentSolvers;
using PADD.StatesDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support
{
	public static class SupportMethods
	{
		private static Logger logger = new Logger();

		public static string benchmarksTestFolder = Environment.GetEnvironmentVariable("SASProblems");

		public static string SAS_allFolder = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL");
		public static string SAS_all_WithoutAxioms = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL_withoutAxioms");
		public static string mediumDomainsFolder = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL - medium");
		public static string mediumDomainsFolderFirstHalf = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL - medium1");
		public static string mediumDomainsFolderSecondHalf = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL - medium2");
		public static string small_and_mediumDomainsFolder = Path.Combine(benchmarksTestFolder, "benchmarksSAS_ALL - small+medium");
		public static string freeLunchSmall = Path.Combine(benchmarksTestFolder, "FreeLunchBenchmarks - small");

		public static string testFilesFolder = Path.Combine(benchmarksTestFolder, "test");
		public static string test2FilesFolder = Path.Combine(benchmarksTestFolder, "test2");

		public static IEnumerable<SASProblem> LoadSASProblemsForDomain(string domainName)
		{
			string folderPath = Path.Combine(SAS_all_WithoutAxioms, domainName);
			var filesOrderedBySize = Directory.EnumerateFiles(folderPath).Where(x => Path.GetExtension(x) == ".sas").Select(q => (q, new FileInfo(q))).OrderBy(q => q.Item2.Length).ToList();
			foreach (var item in filesOrderedBySize)
			{
				yield return SASProblem.CreateFromFile(item.q);
			}
		}

		public static double computeFFHeuristic(string stateString)
		{
			var splitted = stateString.Split('_');
			string domainName = splitted[0];
			string problemName = splitted[1];
			string stateAsString = splitted[2];

			string sasProblemPath = Path.Combine(@"C:\Users\Trunda_Otakar\Documents\Visual Studio 2017\Projects\PADD - NEW\PADD\PADD\bin\tests\benchmarksSAS_ALL_withoutAxioms", domainName, problemName);
			SASProblem p = SASProblem.CreateFromFile(sasProblemPath);
			SASState state = SASState.parse(stateAsString, p);

			FFHeuristic h = new FFHeuristic(p);

			return h.getValue(state);
		}

		public static IHeap<double, IState> getHeapByParam(int param)
		{
			IHeap<double, IState> heapStructure = null;
			switch (param)
			{
				case 1:
					heapStructure = new PADD.Heaps.RedBlackTreeHeap<IState>();
					break;
				case 2:
					heapStructure = new PADD.Heaps.FibonacciHeap1<IState>();
					break;
				case 3:
					heapStructure = new PADD.Heaps.FibonacciHeap2<IState>();
					break;
				case 4:
					heapStructure = new PADD.Heaps.RegularBinaryHeap<IState>();
					break;
				case 5:
					heapStructure = new PADD.Heaps.RegularTernaryHeap<IState>();
					break;
				case 6:
					heapStructure = new PADD.Heaps.BinomialHeap<IState>();
					break;
				case 7:
					heapStructure = new PADD.Heaps.LeftistHeap<IState>();
					break;
				default:
					break;
			}

			return heapStructure;
			//heapStructure = new Heaps.RedBlackTreeHeap<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap1<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap2<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.RegularBinaryHeap<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.RegularTernaryHeap<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.BinomialHeap<IState>();
			//IHeap<double, IState> heapStructure = new Heaps.LeftistHeap<IState>();
		}

		/// <summary>
		/// Recursively enumerates given directory and for every sas+ task it finds, it runs an A* search with given time limit. If the task is not solved in the time limit, the file is removed. If all files from the directory are removed, the directory is removed as well.
		/// </summary>
		/// <param name="domainsFolder"></param>
		/// <param name="timeLimit"></param>
		public static void DeleteTooComplexTasks(string domainsFolder, TimeSpan timeLimit)
		{
			Logger logger = new Logger();
			SASProblem d;
			AStarSearch ast;
			//HillClimbingSearch ast;
			var directories = Directory.EnumerateDirectories(domainsFolder);
			foreach (var directory in directories)
			{
				bool isSomethingSolvedInThisDirectory = false,
					previousFileNotSolved = false,
					removingAllFiles = false;
				var files = Directory.EnumerateFiles(directory);
				foreach (var item in files)
				{
					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + item);
					if (removingAllFiles)
					{
						logger.Log("deleting file " + item);
						File.Delete(item);
						continue;
					}

					d = SASProblem.CreateFromFile(item);
					//ast = new AStarSearch(d, new FFHeuristic(d));
					ast = new AStarSearch(d, null);
					ast.SetHeuristic(new FFHeuristic(d));
					ast.setHeapDatastructure(new PADD.Heaps.RedBlackTreeHeap<IState>());
					ast.timeLimit = timeLimit;
					ast.Search();
					if (ast.GetSearchStatus() != SearchStatus.SolutionFound && ast.GetSearchStatus() != SearchStatus.NoSolutionExist)
					{
						//task not solved. it will be removed
						logger.Log("deleting file " + item);
						File.Delete(item);
						if (previousFileNotSolved)
						{
							//if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
							//removingAllFiles = true;
						}
						previousFileNotSolved = true;
					}
					else
					{
						isSomethingSolvedInThisDirectory = true;
						previousFileNotSolved = false;
					}
					logger.Log();
				}
				if (!isSomethingSolvedInThisDirectory)
				{
					//no task has been solved - removing empty folder
					logger.Log("deleting directory " + directory);
					Directory.Delete(directory);
				}
				logger.Log(" ----- new domain ----- ");
			}

		}

		public static void DeleteTooEasyTasks(string domainsFolder, TimeSpan timeLimit)
		{
			Logger logger = new Logger();
			SASProblem d;
			AStarSearch ast;
			//HillClimbingSearch ast;
			var directories = Directory.EnumerateDirectories(domainsFolder);
			foreach (var directory in directories)
			{
				bool isSomethingSolvedInThisDirectory = false,
					previousFileNotSolved = false,
					removingAllFiles = false;
				var files = Directory.EnumerateFiles(directory);
				foreach (var item in files)
				{
					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + item);
					if (removingAllFiles)
					{
						logger.Log("deleting file " + item);
						File.Delete(item);
						continue;
					}

					d = SASProblem.CreateFromFile(item);
					//ast = new AStarSearch(d, new FFHeuristic(d));
					ast = new AStarSearch(d, null);
					ast.SetHeuristic(new FFHeuristic(d));
					ast.setHeapDatastructure(new PADD.Heaps.RedBlackTreeHeap<IState>());
					ast.timeLimit = timeLimit;
					ast.Search();
					if (ast.GetSearchStatus() == SearchStatus.SolutionFound || ast.GetSearchStatus() == SearchStatus.NoSolutionExist)
					{
						//task solved. it will be removed
						logger.Log("deleting file " + item);
						File.Delete(item);
						if (previousFileNotSolved)
						{
							//if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
							//removingAllFiles = true;
						}
						previousFileNotSolved = true;
					}
					else
					{
						isSomethingSolvedInThisDirectory = true;
						previousFileNotSolved = false;
					}
					logger.Log();
				}
				if (!isSomethingSolvedInThisDirectory)
				{
					//no task has been solved - removing empty folder
					logger.Log("deleting directory " + directory);
					Directory.Delete(directory);
				}
				logger.Log(" ----- new domain ----- ");
			}

		}

		public static void DeleteProblemsWithAxiomRules(string domainsFolder)
		{
			Logger logger = new Logger();
			SASProblem d;

			var directories = Directory.EnumerateDirectories(domainsFolder);
			foreach (var directory in directories)
			{
				bool isSomethingSolvedInThisDirectory = false,
					previousFileNotSolved = false,
					removingAllFiles = false;
				var files = Directory.EnumerateFiles(directory);
				foreach (var item in files)
				{
					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + item);
					if (removingAllFiles)
					{
						logger.Log("deleting file " + item);
						File.Delete(item);
						continue;
					}

					d = SASProblem.CreateFromFile(item);

					if (d.GetAxiomRules().Count > 0)
					{
						//task has axioms. it will be removed
						logger.Log("deleting file " + item);
						File.Delete(item);
						if (previousFileNotSolved)
						{
							//if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
							//removingAllFiles = true;
						}
						previousFileNotSolved = true;
					}
					else
					{
						isSomethingSolvedInThisDirectory = true;
						previousFileNotSolved = false;
					}
					logger.Log();
				}
				if (!isSomethingSolvedInThisDirectory)
				{
					//no task remains here - removing empty folder
					logger.Log("deleting directory " + directory);
					Directory.Delete(directory);
				}
				logger.Log(" ----- new domain ----- ");
			}

		}

		public static void runPlanningExperiments(string domainsFolder, TimeSpan timeLimit, int param)
		{
			Logger logger = new Logger();
			List<SearchResults> allResults = new List<SearchResults>();

			//try
			//{

			//the number of computes that participate on this job. Computation is distributed among them.
			int numberOfComputes = 2;

			//if set to true, problem file that don't have histogram computed will be skipped. Otherwise all problems will be processed.
			bool onlyWhenHistogramExists = false;

			SASProblem d;
			AStarSearch ast;
			//HillClimbingSearch ast;
			var directories = Directory.EnumerateDirectories(domainsFolder);
			allResults = new List<SearchResults>();
			foreach (var directory in directories)
			{
				var files = Directory.EnumerateFiles(directory).ToList();
				foreach (var SASFile in files)
				{
					int indexOfFile = files.IndexOf(SASFile);
					if (indexOfFile % numberOfComputes != param) //if (indexOfFile != param) 
						continue;
					if (onlyWhenHistogramExists && !IsHistogramComputed(directory, SASFile))
						continue;

					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + SASFile);

					d = SASProblem.CreateFromFile(SASFile);

					Heuristic h = new FFHeuristic(d);

					/*
					string histogramFolder = @"C:\Users\Ota\Documents\Visual Studio 2017\Projects\PADD\heuristicStats";
					var samples = HistogramVisualizer.Form1.getHistograms(histogramFolder);
					List<HistogramVisualizer.Histograms> selectedSamples = new List<HistogramVisualizer.Histograms>();
					foreach (var item in samples[Path.GetFileName(directory)].Values)
					{
						selectedSamples.AddRange(item);
					}
					*/

					//string trainedNetworkFile = Path.Combine("..", "trainedNetwork.bin");
					//Heuristic h = new NNHeuristic(d, trainedNetworkFile);

					//string dataFile = Path.Combine(directory, "histograms", "dataToLearn.tsv");
					//Heuristic h = new FileBasedHeuristic(d, dataFile, false);

					//Heuristic h = new FFHeuristic(d);
					//Heuristic h = new RegHeuristic(new FFHeuristic(d));
					//Heuristic h = new FileBasedHeuristic(d, dataFile, false);

					//h = getHeuristicByParam(param, d);
					//h = getHeuristicByParam(6, d);

					ast = new AStarSearch(d, h);
					//ast = new MultipleOpenListsAStar(d, h);
					//ast = new MultipleOpenListsAStar(d, new List<Heuristic>() { h, hNN });
					//ast = new IDAStarSearch(d, null);

					IHeap<double, IState> heapStructure = null;

					//heapStructure = getHeapByParam(param);

					//IHeap<double, IState> heapStructure = new Heaps.MeasuredHeap<IState>();
					heapStructure = new PADD.Heaps.RedBlackTreeHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap1<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap2<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.RegularBinaryHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.RegularTernaryHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.BinomialHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.LeftistHeap<IState>();

					//ast.setHeapDatastructure(heapStructure);

					DirectoryInfo currentDirectory = new DirectoryInfo(directory);
					FileInfo currentFile = new FileInfo(SASFile);

					if (ast.openNodes is PADD.Heaps.MeasuredHeap<IState>)
						((PADD.Heaps.MeasuredHeap<IState>)ast.openNodes).setOutputFile(currentDirectory.Name + "_" + currentFile.Name);

					ast.timeLimit = timeLimit;
					ast.results.domainName = (Path.GetFileName(directory));
					ast.results.problemName = (Path.GetFileName(SASFile));
					ast.results.heuristicName = h.getDescription();
					ast.results.algorithm = ast.getDescription() + "+" + heapStructure.getName();

					ast.Search();
					ast.results.bestHeuristicValue = h.statistics.bestHeuristicValue;
					ast.results.avgHeuristicValue = h.statistics.getAverageHeurValue();

					//foreach (var item in ast.GetSolution().GetOperatorSeqIndices())
					//    Console.Write(item + " ");
					allResults.Add(ast.results);
					if (ast.openNodes is PADD.Heaps.MeasuredHeap<IState>)
						((PADD.Heaps.MeasuredHeap<IState>)ast.openNodes).clearStatistics();
					logger.Log();
				}
				logger.Log(" ----- new domain ----- ");
			}


			//			catch (Exception e)
			//			{
			//			    using (var writer = new System.IO.StreamWriter("results" + Environment.MachineName + ".txt"))
			//			        foreach (var item in allResults)
			//			        {
			//			            writer.WriteLine(item.ToString());
			//			        }
			//}
			using (var writer = new System.IO.StreamWriter("results" + Environment.MachineName + ".txt"))
				foreach (var item in allResults)
				{
					writer.WriteLine(item.ToString());
				}
		}

		/// <summary>
		/// Takes path to a SAS planning problem and determines whether histogram for this file is computed and stored in apropriate folder.
		/// I.e. it checks a folder ./histograms and searches for a file with the same name as the domain file (except the extension).
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="fileName"></param>
		/// <returns>True if the histogram file is found.</returns>
		public static bool IsHistogramComputed(string folderPath, string fileName)
		{
			if (!Directory.Exists(Path.Combine(folderPath, "histograms")))
				return false;
			var histogramFiles = Directory.EnumerateFiles(Path.Combine(folderPath, "histograms"));
			if (histogramFiles.Any(t => Path.GetFileNameWithoutExtension(t) == Path.GetFileNameWithoutExtension(fileName)))
				return true;
			return false;
		}

		public static SearchResults runPlanner(string problem, Heuristic h, bool useTwoQueues = false, int maxTimeMinutes = 10)
		{
			SASProblem p = SASProblem.CreateFromFile(problem);

			if (useTwoQueues)
			{
				MultipleOpenListsAStar engine = new MultipleOpenListsAStar(p, new List<Heuristic>() { h, new FFHeuristic(p) });
				engine.timeLimit = TimeSpan.FromMinutes(maxTimeMinutes);
				var plan = engine.Search();
				engine.results.domainName = Path.GetFileName(Path.GetDirectoryName(problem));
				return engine.results;
			}
			else
			{
				AStarSearch engine = new AStarSearch(p, h);
				//AStarSearch engine = new GreedyBFS(p, h);
				engine.timeLimit = TimeSpan.FromMinutes(maxTimeMinutes);
				var plan = engine.Search();
				engine.results.domainName = Path.GetFileName(Path.GetDirectoryName(problem));
				return engine.results;
			}
		}

		public static void solveDomain(string domainFolder, DomainDependentSolver solver, bool submitPlans = false)
		{
			Console.WriteLine("problem\tminBound\tmaxBound\tplanLength");
			var plansFolder = Path.Combine(domainFolder, "plans");
			if (!Directory.Exists(plansFolder))
				Directory.CreateDirectory(plansFolder);
			foreach (var item in Directory.EnumerateFiles(domainFolder))
			{
				if (Path.GetExtension(item) != ".sas")
					continue;
				solver.SetProblem(SASProblem.CreateFromFile(item));
				var planLength = (int)solver.Search(quiet: true);
				var problemInfo = File.ReadAllLines(Path.Combine(domainFolder, "pddl", "_problemInfo", Path.ChangeExtension(Path.GetFileName(item), "txt"))).Distinct().Select(
					line => line.Split('\t').ToList()).ToDictionary(t => t.First(), t => t.Last());
				int minBound = 0;
				if (!int.TryParse(problemInfo["lowerBound"], out minBound))
					minBound = 0;
				int maxBound = int.MaxValue;
				if (!int.TryParse(problemInfo["upperBound"], out maxBound))
					maxBound = int.MaxValue;
				int problemID = 0;
				if (!problemInfo.ContainsKey("problemID") || !int.TryParse(problemInfo["problemID"], out problemID))
					problemID = -1;
				Console.WriteLine(Path.GetFileNameWithoutExtension(item) + "\t" + minBound + "\t" + maxBound + "\t" + planLength);
				var planFile = Path.Combine(plansFolder, Path.ChangeExtension(Path.GetFileName(item), "txt"));
				if (!File.Exists(planFile) || planLength <= File.ReadAllLines(planFile).Count())
					File.WriteAllLines(planFile, solver.getPDDLPlan());
				if (submitPlans && planLength < maxBound)
				{
					var plan = File.ReadAllLines(planFile).ToList();
					Console.WriteLine("Submiting plan...");
					Console.WriteLine("response:");
					Console.WriteLine("-------------");
					var response = PlanSubmission.submitPlan(plan, problemID);
					Console.WriteLine(response);
					Console.WriteLine("-------------");
				}
			}
		}

		public static void testZenoSolver()
		{
			var problemPath = Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pfile10.sas");
			SASProblem problem = SASProblem.CreateFromFile(problemPath);
			var solver = new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver();
			string stateString = "[1 5 3 1 0 3 4 2x3 4 2 1 0 2 ]";

			var state = SASState.parse(stateString, problem);
			problem.SetInitialState(state);

			solver.SetProblem(problem);
			var res = solver.Search();
			Console.WriteLine(res);
		}

		public static void createStatesDB(string problemFile, DomainDependentSolver domainSpecificSolver)
		{
			var sasProblem = SASProblem.CreateFromFile(problemFile);
			StatesEnumerator e = new RandomWalksFromGoalPathStateSpaceEnumerator(sasProblem, domainSpecificSolver);
			DBCreator c = new DBCreator(e);
			if (domainSpecificSolver is PADD.DomainDependentSolvers.VisitAll.VisitAllSolver)
			{
				var goalPath = ((RandomWalksFromGoalPathStateSpaceEnumerator)e).goalPath;
				((PADD.DomainDependentSolvers.VisitAll.VisitAllSolver)domainSpecificSolver).drawPlan(goalPath);
			}

			c.createDB(problemFile, domainSpecificSolver, 100000, TimeSpan.FromHours(1));
			var states = c.DB.getAllElements().ToList();

			Trie<int> t = Trie<int>.load(c.getDBFilePath(problemFile),
				s => int.Parse(s));
			states = t.getAllElements().ToList();

			var realStates = states.Select(s => (SASState.parse(s.key, sasProblem), s.value)).ToList();
		}

		public static void createStatesDBForDomain(string domainFolder, string outputFolder, DomainDependentSolver solver, long totalSamples, bool storeObjectGraphs = true)
		{
			var problemFiles = PADDUtils.FileSystemUtils.enumerateProblemFiles(domainFolder).ToList();
			long samplesPerFile = totalSamples / problemFiles.Count;

			PADDUtils.FileSystemUtils.createDirIfNonExisting(outputFolder);
			long samplesGenerated = 0;
			long toBeGenerated = samplesPerFile * problemFiles.Count;

			using (var writter = new StreamWriter(Path.Combine(outputFolder, "samples.tsv")))
			{
				long currentID = 1;
				writter.WriteLine("_ID\ttarget\tstate\tdomain\tproblem");   //writing header

				foreach (var item in problemFiles)
				{
					var sasProblem = SASProblem.CreateFromFile(item);
					var initialState = sasProblem.GetInitialState();
					StatesEnumerator e = new RandomWalksFromGoalPathStateSpaceEnumerator(sasProblem, solver);
					DBCreator c = new DBCreator(e);
					var samples = c.createSamples(item, solver, samplesPerFile, TimeSpan.FromHours(5));
					foreach (var sample in samples)
					{
						samplesGenerated++;
						if (samplesGenerated % 100 == 0)
							Console.WriteLine("Samples generated: " + samplesGenerated + " out of " + toBeGenerated + " (" + ((double)samplesGenerated / toBeGenerated * 100).ToString("0.00") + " %");

						writter.Write(currentID + "\t");
						writter.Write(sample.val + "\t");
						writter.Write(sample.key + "\t");
						writter.Write(Path.GetFileName(domainFolder) + "\t");
						writter.WriteLine(Path.GetFileName(item));
						if (storeObjectGraphs)
						{
							SASState s = SASState.parse(sample.key, sasProblem);
							sasProblem.SetInitialState(s);
							var graph = KnowledgeExtraction.computeObjectGraph(sasProblem).toMSAGLGraph();

							/*
							Console.WriteLine(sample.key + "\t" + s.toStringWithMeanings());
							Utils.GraphVisualization.GraphVis.showGraph(graph);
							*/

							string graphPath = Path.Combine(outputFolder, "graphs", currentID.ToString() + ".bin");
							PADDUtils.FileSystemUtils.createDirIfNonExisting(Path.Combine(outputFolder, "graphs"));
							using (var stream = new FileStream(graphPath, FileMode.Create))
							{
								graph.WriteToStream(stream);
							}
							sasProblem.SetInitialState(initialState);
						}
						currentID++;
					}
				}
			}

		}

		public static void visualizeKnowledgeGraphs(string problemFile)
		{
			KnowledgeHolder h = null;
			if (Path.GetExtension(problemFile) == ".sas")
			{
				var sasProblem = SASProblem.CreateFromFile(problemFile);
				h = KnowledgeHolder.compute(sasProblem);
			}
			else
			{
				var domain = Path.Combine(Path.GetDirectoryName(problemFile), "domain.pddl");
				h = KnowledgeHolder.create(PDDLProblem.CreateFromFile(domain, problemFile));
			}
			h.visualize();
		}

		public static void visualizePDDLKnowledgeGraphs(string PDDLDomainFile, string PDDLProblemFile)
		{
			var problem = PDDLProblem.CreateFromFile(PDDLDomainFile, PDDLProblemFile);
			KnowledgeHolder h = KnowledgeHolder.create(problem);
			h.visualize();
		}

		/// <summary>
		/// This function combines all computed resultFiles into a single file.
		/// </summary>
		public static void CombineResultFiles()
		{
			string domainsFolder = SAS_all_WithoutAxioms;
			//string domainsFolder = "../tests/test2";
			gatherResultsFile(domainsFolder);
		}


		/// <summary>
		/// Function for counting number of computed histograms or searchFile results
		/// </summary>
		/// <param name="args"></param>
		public static void CountHistogramsAndResultFiles()
		{
			string domainsFolder = SAS_all_WithoutAxioms;

			var resultsFilesPrefixes = Enum.GetNames(typeof(SearchAlgorithmType)).Select(p => p + "").ToList();
			var counts = resultsFilesPrefixes.Select(pre => countResultsFiles(domainsFolder, pre));

			int totalCount = countAllProblemFiles(domainsFolder);

			foreach (var item in resultsFilesPrefixes.Zip(counts, (pref, count) => (pref, count)))
			{
				Console.WriteLine(item.pref + " results:\t" + item.count + " computed out of " + totalCount);
			}

			//countFF = countHistograms(domainsFolder);
			//Console.WriteLine("Histograms: " + countFF + " computed out of " + totalCount);
		}

		/// <summary>
		/// The function processes all result files found in subfolders of given folder and merges them into a single file.
		/// </summary>
		/// <param name="domainsFolder"></param>
		public static void gatherResultsFile(string domainsFolder)
		{
			string combinedResultsFile = "allResults.tsv";

			var prefixes = Enum.GetNames(typeof(SearchAlgorithmType)).ToList();

			//List<string> resultsFilesPrefixes = new List<string>() { "HeurFF_", "HeurFile_" };
			List<string> resultsFilesPrefixes = prefixes.Select(p => p + "").ToList();

			int numerOfParamsInEachFile = 6;
			using (var writer = new StreamWriter(combinedResultsFile))
			{
				writer.Write("domainFile\tproblemFile\t");
				foreach (var prefix in resultsFilesPrefixes)
				{
					writer.Write(prefix + "totalTime(sec)\t");
					writer.Write(prefix + "expandedNodes\t");
					writer.Write(prefix + "solved?\t");
					writer.Write(prefix + "planLength\t");
					writer.Write(prefix + "minHeurValue\t");
					writer.Write(prefix + "avgHeurValue\t");
				}
				writer.WriteLine();

				foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
				{
					foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
					{
						if (Path.GetExtension(problemFile) != ".sas")
							continue;   //not a planning problem file
						writer.Write(Path.GetFileName(domainFolder) + "\t");
						writer.Write(Path.GetFileNameWithoutExtension(problemFile) + "\t");
						foreach (var prefix in resultsFilesPrefixes)
						{
							string resultFile = Path.Combine(new FileInfo(problemFile).Directory.FullName, "results", prefix + Path.GetFileNameWithoutExtension(problemFile) + ".txt");
							if (!File.Exists(resultFile))
							{
								for (int i = 0; i < numerOfParamsInEachFile; i++)
								{
									writer.Write("\t");
								}
							}
							else
							{
								string[] splittedFileData = File.ReadAllText(resultFile).Split(new string[] { ";", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
								foreach (var item in splittedFileData.Skip(splittedFileData.Length - numerOfParamsInEachFile))
								{
									writer.Write(item + "\t");
								}
							}
						}
						writer.WriteLine();
					}
				}
			}
		}

		/// <summary>
		/// Returns a number that corresponds to total number of computed histograms for all domains in a given folder
		/// </summary>
		/// <param name="domainsFolder"></param>
		public static int countHistograms(string domainsFolder, string histogramPrefix)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) != ".sas")
						continue;
					if (IsHistogramComputed(domainFolder, problemFile))
						totalCount++;
				}
			}
			return totalCount;
		}

		public static int countAllProblemFiles(string domainsFolder)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) == ".sas")
						totalCount++;
				}
			}
			return totalCount;
		}

		public static int countResultsFiles(string domainsFolder, string filePrefix)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) != ".sas")
						continue;
					if (!Directory.Exists(Path.Combine(domainFolder, "results")))
						continue;
					string fileToFind = Path.Combine(domainFolder, "results", filePrefix + Path.GetFileName(Path.ChangeExtension(problemFile, ".txt")));
					if (Directory.EnumerateFiles(Path.Combine(domainFolder, "results")).Any(f => f == fileToFind))
						totalCount++;
				}
			}
			return totalCount;
		}

		/// <summary>
		/// This function copies results (as well as computed histograms) from one tests folder to another, such that the results are on the right place in the second folder.
		/// </summary>
		/// <param name="args"></param>
		public static void copyResultFiles(string folderWithResults, string folderToCopyResultsTo)
		{
			List<string> namesOfFoldersToCopy = new List<string>() { "results", "histograms" };

			var domainsWithResults = new HashSet<string>(Directory.EnumerateDirectories(folderWithResults).Select(dir => Path.GetFileName(dir)));

			foreach (var domainFolderName in Directory.EnumerateDirectories(folderToCopyResultsTo).Select(dir => Path.GetFileName(dir)))
			{
				if (domainsWithResults.Contains(domainFolderName))
				{
					var folderNamesInSource = Directory.EnumerateDirectories(Path.Combine(folderWithResults, domainFolderName)).Select(dir => Path.GetFileName(dir));
					foreach (var folderToCopy in namesOfFoldersToCopy)
					{
						if (folderNamesInSource.Contains(folderToCopy))
						{
							DirectoryCopy(Path.Combine(folderWithResults, domainFolderName, folderToCopy), Path.Combine(folderToCopyResultsTo, domainFolderName, folderToCopy), true);
						}
					}
				}
			}
		}

		private static void DirectoryCopy(string sourceDirPath, string destDirPath, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirPath);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirPath))
			{
				Directory.CreateDirectory(destDirPath);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirPath, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirPath, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

	}

	public enum SearchAlgorithmType
	{
		heurFF,
		heurFile_Median,
		heurFile_Mean,
		doubleListFF_FileMedian,
		doubleListFF_FileMean,
	}

	public enum HeuristicType
	{
		FF,
		net,
		sum,
		max,
		min,
		weighted10,
		doubleList,
		domainSolver_NN
	}

	public enum StatesHistogramType
	{
		heurFF_Median,
		heurFF_Mean,
	}
}
