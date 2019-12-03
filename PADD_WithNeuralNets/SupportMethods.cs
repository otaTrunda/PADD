using NeuralNetTrainer;
using PADD;
using PADD.DomainDependentSolvers;
using PADD.StatesDB;
using PADD_Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.ExtensionMethods;
using Utils.MachineLearning;
using PAD.Planner.Heuristics;
using PAD.Planner.Search;
using PAD.Planner.SAS;
using GraphUtils.Graphs;
using GraphUtils.GraphFeatureGeneration;
using PADD_Support.KnowledgeExtraction;
using GraphUtils;

namespace PADD_WithNeuralNets
{
	static class SupportMethods
	{
		static Logger logger = new Logger();

		/// <summary>
		/// The method takes the given problem and enumerates its state-space.
		/// If rewrite is set to true, new histograms will be created even if they already exist, is it is set to false, domains, where there already are histograms, will be skipped.
		/// </summary>
		public static void createHeuristic2distanceStatictics(string domainFolder, int param, int computers, DateTime reWriteIfOlderThan)
		{
			logger.Log("Param is " + param);
			var files = Directory.EnumerateFiles(domainFolder).ToList();
			int numberOfComputers = computers;
			foreach (var item in files)
			{
				if (numberOfComputers == 1 && files.IndexOf(item) != param)
				{
					logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
					continue;
				}

				if (numberOfComputers > 1 && files.IndexOf(item) % numberOfComputers != param)
				{
					logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
					continue;
				}

				string problemFile = item;
				logger.Log("Processing file " + item);

				var d = new Problem(problemFile, false);
				List<Heuristic> heuristics = new List<Heuristic>() { new FFHeuristic(d) };


				string domainName = Path.GetFileName(domainFolder);
				string probleName = Path.GetFileName(item);

				/*
				List<int> solution = new List<int>() { 3, 5, 14, 13, 12, 10, 1, 16 };
				List<SASOperator> allOps = d.GetOperators();

				var s = d.GetInitialState();
				foreach (var opIndex in solution)
				{
					SASOperator op = allOps[opIndex];
					logger.Log();
					logger.Log(string.Join(" ", ((SASState)(s)).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					if (!op.IsApplicable(s))
						throw new Exception();
					s = op.Apply(s);
				}
				if (s.IsMeetingGoalConditions())
				{
					logger.Log("huraa");
				}
				*/

				string resultsPath = Path.Combine(domainFolder, "histograms");

				if (!Directory.Exists(resultsPath))
					Directory.CreateDirectory(resultsPath);

				string resultFile = Path.Combine(resultsPath, Path.ChangeExtension(probleName, "txt"));
				if (File.Exists(resultFile) && File.GetCreationTime(resultFile) >= reWriteIfOlderThan)
				{
					logger.Log("Skipping domain " + domainName + " , file " + probleName + " - histogram already exists and it's up to date (" + File.GetCreationTime(resultFile).ToLongDateString() + ")");
					continue;
				}

				logger.Log("Processing domain " + domainName + " , file " + probleName + (File.Exists(resultFile) ? "\tfile exists but it's old (" + File.GetCreationTime(resultFile).ToLongDateString() + ")" : ""));

				var histogram = StateSpaceHistogramCalculator.getHistogram(problemFile, heuristics);

				StateSpaceHistogramCalculator.writeHistograms(resultFile, histogram);
				logger.Log("Histogram successfully written to " + resultFile);
				//StateSpaceHistogramCalculator.writeHistograms(domainName + "_" + Path.ChangeExtension(probleName, "txt"), histogram);
			}
		}

		/// <summary>
		/// Main function for creating statistics of heuristic values compared to real goal-distances. I.e. for creating "dataToLearn.tsv" file by combining existing histograms.
		/// </summary>
		public static void CombineHistogramFiles()
		{
			/*
			FeaturesCalculator.countHistograms(small_and_mediumDomainsFolder);
			return;
			*/

			/*
			string folder = mediumDomainsFolderSecondHalf;
			int domainID = int.Parse(args[0]);
            if (Directory.EnumerateDirectories(folder).ToList().Count <= domainID)
                return;
            string domainFolder = Directory.EnumerateDirectories(folder).ToList()[domainID];

			int numberOfProblems = Directory.EnumerateFiles(domainFolder).Count();
            for (int i = 0; i < numberOfProblems; i++)
            {
                createHeuristic2distanceStatictics(domainFolder, i);
            }
			return;
			*/
			//processAllHistogramFolders(testFilesFolder);

			DateTime reWriteIfOlderThan = DateTime.Now;
			processAllHistogramFolders(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, reWriteIfOlderThan);
		}

		/// <summary>
		/// Runs planner on the set of problems creating a file with results. Separate results file is created for every input problem.
		/// </summary>
		/// <param name="domainFolder"></param>
		/// <param name="problemNumber"></param>
		/// <param name="reWrite"></param>
		/// <param name="timeLimit"></param>
		public static void createPlannerResultsStatistics(string domainFolder, int problemNumber, int numberOfComputersUsed, DateTime reWriteIfOlderThan, TimeSpan timeLimit)
		{
			logger.Log("Param is " + problemNumber);
			var files = Directory.EnumerateFiles(domainFolder).ToList();
			int numberOfComputers = numberOfComputersUsed;

			var types = Enum.GetValues(typeof(SearchAlgorithmType));

			foreach (SearchAlgorithmType typeItem in types)
			{
				SearchAlgorithmType type = typeItem;
				//SearchAlgorithmType type = SearchAlgorithmType.heurFile_Median;

				Console.WriteLine("Number of computers: " + numberOfComputers);
				foreach (var item in files)
				{
#if DEBUG
					if (numberOfComputersUsed == 1 && files.IndexOf(item) != problemNumber)
						continue;
#endif

					if (numberOfComputersUsed > 1 && files.IndexOf(item) % numberOfComputers != problemNumber)
					{
						logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
						continue;
					}

					string problemFile = item;
					logger.Log("Processing file " + item);

					var d = new Problem(problemFile, false);

					string domainName = Path.GetFileName(domainFolder);
					string probleName = Path.GetFileName(item);
					string resultsPath = Path.Combine(domainFolder, "results");

					if (!Directory.Exists(resultsPath))
						Directory.CreateDirectory(resultsPath);

					string resultFile = Path.Combine(resultsPath, type.ToString() + Path.GetFileNameWithoutExtension(probleName) + ".txt");

					if (File.Exists(resultFile) && File.GetCreationTime(resultFile) >= reWriteIfOlderThan)
					{
						logger.Log("Skipping domain " + domainName + " , file " + probleName + " - resultsFile already exists and is up to date (" + File.GetCreationTime(resultFile).ToLongDateString() + ")");
						continue;
					}

					logger.Log("Processing domain " + domainName + " , file " + probleName + (File.Exists(resultFile) ? "\tresultsFile exists but it's old (" + File.GetCreationTime(resultFile).ToLongDateString() + ")" : ""));

					string dataFile = "";
					switch (type)
					{
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.heurFile_Mean:
							dataFile = Path.Combine(domainFolder, "histograms", "dataToLearn_Mean.tsv");
							break;
						case SearchAlgorithmType.doubleListFF_FileMedian:
						case SearchAlgorithmType.heurFile_Median:
							dataFile = Path.Combine(domainFolder, "histograms", "dataToLearn_Median.tsv");
							break;
					}

					Heuristic h = null;

					switch (type)
					{
						case SearchAlgorithmType.heurFF:
							h = new FFHeuristic(d);
							break;
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.doubleListFF_FileMedian:
						case SearchAlgorithmType.heurFile_Mean:
						case SearchAlgorithmType.heurFile_Median:
							h = new FileBasedHeuristic(d, dataFile, false);
							break;
					}

					AStarSearch ast = null;

					switch (type)
					{
						case SearchAlgorithmType.heurFF:
						case SearchAlgorithmType.heurFile_Mean:
						case SearchAlgorithmType.heurFile_Median:
							ast = new AStarSearch(d, h);
							break;
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.doubleListFF_FileMedian:
							ast = new MultiHeuristicAStarSearch(d, new List<IHeuristic>() { h, new FFHeuristic(d) });
							break;
					}

					ast.TimeLimitOfSearch = timeLimit;
					ast.Start();

					var result = ast.GetSearchResults();
					result.DomainName = domainName;
					result.ProblemName = probleName;
					result.Heuristic = h.GetDescription();
					result.Algorithm = ast.GetDescription() + "+" + ast.OpenNodes.GetName();
					result.BestHeuristicValue = h.Statistics.BestHeuristicValue;
					result.AverageHeuristicValue = h.Statistics.AverageHeuristicValue;

#if DEBUG
					bool printPlan = true;
					if (printPlan)
					{
						Console.WriteLine("Plan:");
						var state = d.GetInitialState();
						Console.WriteLine(state.ToString());
						foreach (var op in result.SolutionPlan)
						{
							state = op.Apply(state);
							Console.WriteLine(state.ToString() + "\toperator aplied: " + op.ToString());
						}
					}
#endif
					using (var writer = new System.IO.StreamWriter(resultFile))
						writer.WriteLine(result.ToString());

					logger.Log("Results successfully written to " + resultFile);
				}
			}
		}

		/// <summary>
		/// Runs planner on all problems from given domain and return results statistics.
		/// </summary>
		/// <param name="domainName"></param>
		/// <param name="ht"></param>
		/// <param name="time"></param>
		/// <param name="hFactory"></param>
		/// <returns></returns>
		public static IEnumerable<SearchResults> runPlanner(string domainName, HeuristicType ht, TimeSpan time, FFNetHeuristicFactory hFactory, List<List<(TrainingSample, double)>> additionalSamplesPlaceholder = null)
		{
			//List<SearchResults> allResults = new List<SearchResults>();

			Problem d;
			AStarSearch ast = null;
			int problemsCount = 0;
			DomainType type = default;
			if (domainName == "zenotravel")
			{
				problemsCount = 21;
				type = DomainType.Zeno;
			}
			if (domainName == "blocks")
			{
				problemsCount = 27;
				type = DomainType.Blocks;
			}

			var problems = PADD_Support.SupportMethods.LoadSASProblemsForDomain(domainName);
			int problemID = 0;
			//HillClimbingSearch ast;
			Utils.Transformations.LinearMapping noiseMap = new Utils.Transformations.LinearMapping(0, problemsCount, WeightedSumHeuristicMaxNoice, 0);
			Utils.Transformations.Mapping weightMap = new Utils.Transformations.ExpMapping(0, problemsCount, WeightedSumHeuristicMinWeight, WeightedSumHeuristicMaxWeight, 1.3d);

			foreach (var problem in problems)
			{
				problemID++;
				d = problem;
				Heuristic h = null;
				if (hFactory != null)
				{
					h = hFactory.create(problem);
				}
				switch (ht)
				{
					case HeuristicType.FF:
						h = new FFHeuristic(problem);
						break;
					case HeuristicType.domainSolver_NN:
						var q = int.Parse(Path.GetFileNameWithoutExtension(hFactory.featuresGenPath).Split('_').Last());
						h = new WeightedSumHeuristic(new List<Tuple<IHeuristic, double>>() { Tuple.Create((IHeuristic)new NoisyPerfectHeuristic(problem, type, noiseMap.getVal(problemID)), (q - 1) * weightMap.getVal(problemID)), Tuple.Create((IHeuristic)h, 1.0) });
						break;
					case HeuristicType.net:
						//nothing here
						break;
					default:
						throw new Exception();
				}

				ast = new AStarSearch(d, h);

				ast.TimeLimitOfSearch = time;
				ast.Start();

				var result = ast.GetSearchResults();
				result.DomainName = domainName;
				result.ProblemName = Path.GetFileName(problem.GetInputFilePath());
				result.Heuristic = h.GetDescription();
				result.Algorithm = ast.GetDescription() + "+" + ast.OpenNodes.GetName();
				result.BestHeuristicValue = h.Statistics.BestHeuristicValue;
				result.AverageHeuristicValue = h.Statistics.AverageHeuristicValue;

				yield return result;
				if (h is SimpleFFNetHeuristic && additionalSamplesPlaceholder != null)
				{
					additionalSamplesPlaceholder.Add(((SimpleFFNetHeuristic)h).newSamples);
				}
			}
			//return allResults;
		}

		// [pozn. HurtT]: tyto polozky byly puvodne ve WeightedSumHeuristic, ale jsou to nejake specificke veci a ani se nikde neinituji?
		public static double WeightedSumHeuristicMaxNoice, WeightedSumHeuristicMaxWeight, WeightedSumHeuristicMinWeight;

		/// <summary>
		/// Main function for creating heuristic-distance histograms OR planners results statistics
		/// </summary>
		/// <param name="args"></param>
		public static void CreateHistograms_Results(string[] args)
		{
			/*
			string Domain = SAS_all_WithoutAxioms + "/logistics00";
			//string Domain = small_and_mediumDomainsFolder + "/tidybotFL";
			int problemNum = 22;
			createHeuristic2distanceStatictics(Domain, problemNum, computers: 1, reWriteIfOlderThan: DateTime.Now);
			//createPlannerResultsStatistics(Domain, problemNum, numberOfComputersUsed: 1, reWriteIfOlderThan: DateTime.Now, timeLimit: TimeSpan.FromMinutes(15));
			return;
			*/

			string domainsFolder = PADD_Support.SupportMethods.SAS_all_WithoutAxioms;
			//string domainsFolder = "./../tests/test2";

			DateTime reWriteIfOlderThan = new DateTime(2018, 3, 9);
			//DateTime reWriteIfOlderThan = DateTime.Now;

			int problemNumber = int.Parse(args[0]) - 1;
			int numberOfComputers = int.Parse(args[1]);
			//int problemNumber = 1;

			//foreach (var domain in Directory.EnumerateDirectories(domainsFolder))
			//foreach (var domain in Directory.EnumerateDirectories(domainsFolder).Reverse()) //reverse the order of domains in order to skip some problematic instances
			//var domainFolders = shuffleList(Directory.EnumerateDirectories(domainsFolder).ToList(), new Random(problemNumber));
			var domainFolders = Utils.ExtensionMethods.RandomExtensionsAndShuffle.Randomly(Directory.EnumerateDirectories(domainsFolder).ToArray(), new Random());

			foreach (var domain in domainFolders)
			{
				//createHeuristic2distanceStatictics(domain, problemNumber, numberOfComputers, reWrite);
				createPlannerResultsStatistics(domain, problemNumber, numberOfComputers, reWriteIfOlderThan, TimeSpan.FromMinutes(30));
			}
			return;
		}


		/// <summary>
		/// Reads all folders in given folder, searches for folders named "histograms". Inside these folders it finds all histogram files and merges them into a "dataToLearn.tsv" file that can then be used for training a ML model.
		/// These files will be created in every respective directory. If the ".tsv" file already exists, it will be rewritten only if it is older than <paramref name="reWriteIfOlderThan"/>.
		/// </summary>
		public static void processAllHistogramFolders(string domainsFolderPath, DateTime reWriteIfOlderThan)
		{
			Logger logger = new Logger();
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolderPath))
			{
				string histogramFolder = Path.Combine(domainFolder, "histograms");
				if (Directory.Exists(histogramFolder))
				{
					logger.Log("processing folder " + histogramFolder);
					foreach (StatesHistogramType item in Enum.GetValues(typeof(StatesHistogramType)))
					{
						FeaturesCalculator.processHistogramsFolder(histogramFolder, reWriteIfOlderThan, item);
					}
					logger.Log("done");
				}
			}
		}

		/// <summary>
		/// Takes a folder containing trainied networks and runs experiments using them as heuristics. Stores Results into same folders (if they are not present already)
		/// </summary>
		/// <param name="networksFolder"></param>
		public static void runNetworks(string domain, int runnerID, int runnersTotal, bool storeAdditionalSamples, int timeMinutes = 30, string architecture = "good4", HeuristicType type = HeuristicType.net)
		{
			string resultsFileName = "searchResults.txt";
			string netFileName = "trainedNet.bin";
			string collectionDir = Path.Combine(@"B:\SAS_Data\", domain, "uniqueSamples");
			string additionalSamplesDir = Path.Combine(@"B:\SAS_Data\", domain, "additionalSamples");
			string additionalSamplesFile = Path.Combine(additionalSamplesDir, "samples_" + DateTime.Now.Ticks + ".tsv");
			if (storeAdditionalSamples && !Directory.Exists(additionalSamplesDir))
			{
				Directory.CreateDirectory(additionalSamplesDir);
			}

			foreach (var networksFolder in Directory.EnumerateDirectories(collectionDir))
			{
				int fileID = -1;
				foreach (var item in Directory.EnumerateDirectories(networksFolder).Where(w => Path.GetFileName(w).Contains(architecture)))
				{
					fileID++;
					if (fileID % runnersTotal != runnerID)
					{
						Console.WriteLine("skipping file " + item);
						continue;
					}
					Console.WriteLine("--------------------------");
					Console.WriteLine();
					Console.WriteLine("processing file " + item);
					string resPath = Path.Combine(item, resultsFileName);

					if (File.Exists(resPath) && false)
						continue;

					FFNetHeuristicFactory factory = null;
					List<List<(TrainingSample, double)>> additionalSamples = null;
					List<SearchResults> results = new List<SearchResults>();

					if (type != HeuristicType.FF)
					{
						string netPath = Path.Combine(item, netFileName);
						if (!File.Exists(netPath))
							continue;
						var net = Network.load(netPath);
												
						int q = int.Parse(Path.GetFileNameWithoutExtension(item).Split("_").First());
						bool useFF = Path.GetFileNameWithoutExtension(item).Split("_").Skip(1).First() == "ff";
						string featuresGenPath = Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_" + q + ".bin");

						factory = new FFNetHeuristicFactory(featuresGenPath, netPath, useFF, TargetTransformationType.none);
						if (storeAdditionalSamples)
						{
							int q2GeneratorVectorSize = Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_2.bin")).vectorSize();
							int q3GeneratorVectorSize = Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_3.bin")).vectorSize();

							DomainDependentSolver s = null;
							if (domain == "zenotravel")
								s = new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver();
							if (domain == "blocks")
								s = new PADD.DomainDependentSolvers.BlocksWorld.BlocksWorldSolver();
							factory = new FFNetHeuristicFactory(featuresGenPath, netPath, useFF, TargetTransformationType.none, s,
								Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_4.bin"));
						}
						if (storeAdditionalSamples)
							additionalSamples = new List<List<(TrainingSample, double)>>();
					}
					foreach (var res in runPlanner(domain, type, TimeSpan.FromMinutes(timeMinutes), factory, additionalSamples))
					{
						File.AppendAllLines(resPath, res.ToString().Yield());
						results.Add(res);
					}
					//File.WriteAllLines(resPath, results.Select(w => w.ToString()));
					Console.WriteLine("search finished");
					if (storeAdditionalSamples)
					{
						int q2GeneratorVectorSize = Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_2.bin")).vectorSize();
						int q3GeneratorVectorSize = Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(Path.Combine(networksFolder, "..", "..", "featuresGen", "generator_3.bin")).vectorSize();
						var samplesWithRes = additionalSamples.Zip(results).Where(w => w.Item2.ResultStatus != ResultStatus.SolutionFound);
						using (var writter = new StreamWriter(additionalSamplesFile, append: true))
							foreach (var newSamps in samplesWithRes)
							{
								foreach (var newSamp in newSamps.Item1.Where(w => Math.Max((w.Item1.targets.Single() + 1) / (w.Item2 + 1), (w.Item2 + 1) / (w.Item1.targets.Single() + 1)) > 2))
								{
									writter.Write(newSamp.Item1.userData + "\t");
									writter.Write((int)newSamp.Item1.targets.Single() + "\t");
									writter.Write(string.Join(",", newSamp.Item1.inputs.Take(q2GeneratorVectorSize)) + "\t");
									writter.Write(string.Join(",", newSamp.Item1.inputs.Take(q3GeneratorVectorSize)) + "\t");
									writter.Write(string.Join(",", newSamp.Item1.inputs.Take(newSamp.Item1.inputs.Length - 1)) + "\t");
									writter.WriteLine((int)newSamp.Item1.inputs.Last());
								}
							}
					}
				}
			}

		}

		/// <summary>
		/// Goes through training logs of various networks and prints test error of those trained nets. (to select the best one).
		/// </summary>
		/// <param name="domain"></param>
		public static void printTestErrors(string domain, bool useFullGenerators)
		{
			string collectionDir = Path.Combine(@"B:\SAS_Data\", domain, "uniqueSamples" + (useFullGenerators ? "FULL" : ""));
			string logFileName = "trainingLog.tsv";

			foreach (var networksFolder in Directory.EnumerateDirectories(collectionDir))
			{
				Console.WriteLine("------------");
				Console.WriteLine(networksFolder);
				Console.WriteLine("folder\tminTestErr\tTrainErr\tepoch\tMSE\t80%-q\t90%-q\t95%-q");
				foreach (var item in Directory.EnumerateDirectories(networksFolder))
				{
					string resPath = Path.Combine(item, logFileName);
					if (!File.Exists(resPath))// && false)
						continue;
					var data = Utils.IO.IO_Utils.readTSV(resPath, true);
					var minElement = data.ArgMin(q => q[2]);
					var bestTestErr = minElement[2];
					var trainingErr = minElement[1];
					var epoch = minElement[0];
					Console.Write(Path.GetFileNameWithoutExtension(item) + "\t");
					Console.Write(bestTestErr + "\t");
					Console.Write(trainingErr + "\t");
					Console.WriteLine(epoch + "\t");

					string fullResPath = Path.Combine(item, "netResults.tsv");
					var errorMeasures = getErrors(fullResPath);
					foreach (var err in errorMeasures)
					{
						Console.Write(err + "\t");
					}
					Console.WriteLine();

				}
			}
		}

		/// <summary>
		/// Returns error measures on given netResults file
		/// </summary>
		/// <param name="fileWithResults"></param>
		/// <returns></returns>
		public static List<double> getErrors(string fileWithResults)
		{
			var data = Utils.IO.IO_Utils.readTSV(fileWithResults, true).Select(q => (double.Parse(q[2]), double.Parse(q[3]))).ToList();
			return getErrors(data);
		}

		/// <summary>
		/// Calculates error measures on given data
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static List<double> getErrors(List<(double target, double output)> data)
		{
			double MSE = data.Select(q => Math.Pow(q.output - q.target, 2)).Sum() / data.Count;
			var absDiffsSorted = data.Select(q => Math.Abs(q.output - q.target)).OrderBy(q => q).ToList();
			var quantile80 = absDiffsSorted[(int)(data.Count * 0.8d)];
			var quantile90 = absDiffsSorted[(int)(data.Count * 0.9d)];
			var quantile95 = absDiffsSorted[(int)(data.Count * 0.95d)];
			return new List<double>() { MSE, quantile80, quantile90, quantile95 };
		}

		public static void createSamplesByHistogram(string domainName, int samplesCount = 1000000)
		{
			Random r = new Random(123);

			string domainDirectory = @"B:\SAS_Data\" + domainName;
			string histogramDir = Path.Combine(domainDirectory, "histogram");
			var histogramPath = Path.Combine(histogramDir, "generatedSamplesHist" + samplesCount + ".tsv");
			var statesPath = Path.Combine(domainDirectory, "uniqueStates");

			if (!File.Exists(histogramPath))
				sampleStates(domainName, samplesCount);

			string outputDir = Path.Combine(domainDirectory, "sampledStates");
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			string outputSubDir = Path.Combine(outputDir, "c_" + samplesCount.ToString());
			if (!Directory.Exists(outputSubDir))
				Directory.CreateDirectory(outputSubDir);

			var hist = Utils.IO.IO_Utils.readTSV(histogramPath).Select(q => (file: q[0], dist: int.Parse(q[1]), count: int.Parse(q[2]))).ToList();
			Dictionary<string, Dictionary<int, int>> statesByFileByDist = hist.GroupBy(q => q.file).Select(q => (q.Key, q.GroupBy(t => t.dist).ToDictionary(w => w.Key, w => w.Single().count))).ToDictionary(w => w.Key, w => w.Item2);

			foreach (var fileName in statesByFileByDist.Keys)
			{
				var outputFilePath = Path.Combine(outputSubDir, fileName + ".tsv");
				if (File.Exists(outputFilePath))
					continue;
				Console.WriteLine("processing file " + fileName);
				var statesFilePath = Path.Combine(statesPath, fileName + ".tsv");
				var states = Utils.IO.IO_Utils.readTSV(statesFilePath).Select(q => (state: q[0], dist: int.Parse(q[1])));
				var statesByDist = states.GroupBy(q => q.dist).ToDictionary(q => q.Key, q => q.ToList());
				states = null;

				using (var writter = new StreamWriter(outputFilePath))
				{
					foreach (var distance in statesByFileByDist[fileName].Keys)
					{
						var relevantStates = statesByDist[distance].ToArray();
						relevantStates.Shuffle(r);
						for (int i = 0; i < statesByFileByDist[fileName][distance]; i++)
						{
							var selectedState = relevantStates[i % relevantStates.Length];
							writter.WriteLine(selectedState.state + "\t" + selectedState.dist);
						}
					}
				}
			}
		}

		/// <summary>
		/// Samples states from all available states to create a set of training samples. Sampling is done in such a way that all files and all classes are present relativly same often.
		/// </summary>
		/// <param name="domainName"></param>
		/// <returns></returns>
		public static void sampleStates(string domainName, long samplesToGenerate = 1000000)
		{
			Random r = new Random(123);

			string domainDirectory = @"B:\SAS_Data\" + domainName;
			string histogramDir = Path.Combine(domainDirectory, "histogram");
			var histogramPath = Path.Combine(histogramDir, "hist.tsv");

			if (!File.Exists(histogramPath))
				storeStatesHistogram(domainName);

			var hist = Utils.IO.IO_Utils.readTSV(histogramPath).Select(q => (file: q[0], dist: int.Parse(q[1]), count: int.Parse(q[2]))).ToList();

			Dictionary<string, Dictionary<int, int>> statesByFileByDist = hist.GroupBy(q => q.file).Select(q => (q.Key, q.GroupBy(t => t.dist).ToDictionary(w => w.Key, w => w.Single().count))).ToDictionary(w => w.Key, w => w.Item2);
			Dictionary<int, Dictionary<string, int>> statesByDistByFile = hist.GroupBy(q => q.dist).Select(q => (q.Key, q.GroupBy(t => t.file).ToDictionary(w => w.Key, w => w.Single().count))).ToDictionary(w => w.Key, w => w.Item2);
			Dictionary<string, int> statesByFile = hist.GroupBy(q => q.file).Select(q => (q.Key, q.Select(t => t.count).Sum())).ToDictionary(q => q.Key, q => q.Item2);
			Dictionary<int, int> statesByLength = hist.GroupBy(q => q.dist).Select(q => (q.Key, q.Select(t => t.count).Sum())).ToDictionary(q => q.Key, q => q.Item2);

			Dictionary<string, double> SQRTstatesByFile = statesByFile.MapValues(q => Math.Sqrt(q));
			double SQRTstatesByFileSum = SQRTstatesByFile.Values.Sum();
			Dictionary<string, int> expectedSamplesByFile = SQRTstatesByFile.MapValues(q => (int)Math.Round(q / SQRTstatesByFileSum * samplesToGenerate));

			Dictionary<int, double> logStatesByLength = statesByLength.MapValues(q => Math.Log(q));
			double logStatesByLengthSum = logStatesByLength.Values.Sum();
			Dictionary<int, int> expectedSamplesByLength = logStatesByLength.MapValues(q => (int)Math.Round(q / logStatesByLengthSum * samplesToGenerate));

			Dictionary<string, int> samplesByFile = statesByFileByDist.Keys.ToDictionary(q => q, q => 0);
			Dictionary<int, int> samplesByLength = statesByDistByFile.Keys.ToDictionary(q => q, q => 0);

			Dictionary<(string file, int length), int> samplesGeneratedByAttributes = new Dictionary<(string file, int length), int>();
			long samplesGenerated = 0;

			foreach (var file in statesByFileByDist.Keys)
			{
				var lengthsPresent = statesByFileByDist[file].Keys.ToList();
				var statesByDistsToGenerate = lengthsPresent.ToDictionary(q => q, q => expectedSamplesByLength[q]);
				var sumS = statesByDistsToGenerate.Select(q => q.Value).Sum();
				statesByDistsToGenerate = statesByDistsToGenerate.MapValues(q => (int)Math.Round((double)q / sumS * expectedSamplesByFile[file] * 3d / 4));
				foreach (var item in statesByDistsToGenerate)
				{
					for (int i = 0; i < item.Value; i++)
					{
						samplesGeneratedByAttributes.AddModify((file, item.Key), 1, (a, b) => a + b);
						samplesByFile.AddModify(file, 1, (a, b) => a + b);
						samplesByLength.AddModify(item.Key, 1, (a, b) => a + b);
						samplesGenerated++;
					}
				}
			}

			while (samplesGenerated < samplesToGenerate)
			{
				{
					var forbiddenDistancesForFile = new HashSet<int>();
					var fileCandidates = samplesByFile.Keys.Select(k => (k, (double)samplesByFile[k] / expectedSamplesByFile[k])).MinElements(q => q.Item2);
					//var fileCandidates = samplesByFile.Select(q => (k: q.Key, q.Value)).ToList();
					var mostUrgentFile = fileCandidates[r.Next(fileCandidates.Count)].k;
					var urgentDist = statesByFileByDist[mostUrgentFile].Keys.Select(k => (k, (double)samplesByLength[k] / expectedSamplesByLength[k])).ArgMin(q => q.Item2).k;

					while (Math.Max(statesByFileByDist[mostUrgentFile][urgentDist] * 1.5, 20) < samplesGeneratedByAttributes[(mostUrgentFile, urgentDist)])
					{
						forbiddenDistancesForFile.Add(urgentDist);
						urgentDist = statesByFileByDist[mostUrgentFile].Keys.Where(k => !forbiddenDistancesForFile.Contains(k)).Select(k => (k, (double)samplesByLength[k] / expectedSamplesByLength[k])).ArgMin(q => q.Item2).k;
					}

					samplesGeneratedByAttributes.AddModify((mostUrgentFile, urgentDist), 1, (a, b) => a + b);
					samplesByFile.AddModify(mostUrgentFile, 1, (a, b) => a + b);
					samplesByLength.AddModify(urgentDist, 1, (a, b) => a + b);
					samplesGenerated++;
				}

				{
					var forbiddenFileForDistance = new HashSet<string>();

					var distanceCandidates = samplesByLength.Keys.Select(k => (k, (double)samplesByLength[k] / expectedSamplesByLength[k])).MinElements(q => q.Item2);
					var mostUrgentDist = distanceCandidates[r.Next(distanceCandidates.Count)].k;
					var urgentFile = statesByDistByFile[mostUrgentDist].Keys.Select(k => (k, (double)samplesByFile[k] / expectedSamplesByFile[k])).ArgMin(q => q.Item2).k;

					while (urgentFile != null && Math.Max(statesByFileByDist[urgentFile][mostUrgentDist] * 1.5, 20) < samplesGeneratedByAttributes[(urgentFile, mostUrgentDist)])
					{
						forbiddenFileForDistance.Add(urgentFile);
						urgentFile = statesByDistByFile[mostUrgentDist].Keys.Where(q => !forbiddenFileForDistance.Contains(q)).Select(k => (k, (double)samplesByFile[k] / expectedSamplesByFile[k])).ArgMin(q => q.Item2).k;
					}
					if (urgentFile == null)
						continue;

					samplesGeneratedByAttributes.AddModify((urgentFile, mostUrgentDist), 1, (a, b) => a + b);
					samplesByFile.AddModify(urgentFile, 1, (a, b) => a + b);
					samplesByLength.AddModify(mostUrgentDist, 1, (a, b) => a + b);
					samplesGenerated++;
				}
			}

			File.WriteAllLines(Path.Combine(histogramDir, "generatedSamplesHist" + samplesToGenerate + ".tsv"), samplesGeneratedByAttributes.Select(q => q.Key.file + "\t" + q.Key.length + "\t" + q.Value));
		}

		/// <summary>
		/// Goes through all stored states and produces a histogram of their target values. Requires that unique states have already been stored.
		/// </summary>
		/// <param name="domainName"></param>
		public static void storeStatesHistogram(string domainName)
		{
			string domainDirectory = @"B:\SAS_Data\" + domainName;
			string histogramDir = Path.Combine(domainDirectory, "histogram");
			if (!Directory.Exists(histogramDir))
				Directory.CreateDirectory(histogramDir);
			string statesDir = Path.Combine(domainDirectory, "uniqueStates");

			using (var writter = new StreamWriter(Path.Combine(histogramDir, "hist.tsv")))
			{
				foreach (var item in Utils.IO.Directory.EnumerateFilesBySize(statesDir))
				{
					Console.WriteLine("processing file " + item + " at " + DateTime.Now);
					string fileName = Path.GetFileNameWithoutExtension(item);
					var allVals = File.ReadLines(item).Select(q => q.Split("\t")[1]).Select(q => int.Parse(q));
					var histogram = allVals.HistogramOfValues();
					foreach (var val in histogram)
					{
						writter.WriteLine(fileName + "\t" + val.Key + "\t" + val.Value);
					}
				}
			}
		}

		/// <summary>
		/// Goes through previsously generated samples and produces files that contain only uniques samples (actually, a small number of duplicates is allowed for each sample)
		/// </summary>
		/// <param name="domainName"></param>
		public static void filterUniqueTrainigSamples(string domainName, int workerIndex, int totalWorkers, bool useFullGenerator = false)
		{
			string domainDirectory = @"B:\SAS_Data\" + domainName;
			string samplesDirName = "samples";
			if (useFullGenerator) samplesDirName = "samplesFULL";
			string samplesDir = Path.Combine(domainDirectory, samplesDirName);
			string statesDir = Path.Combine(domainDirectory, "sampledStates");
			string uniqueSamplesDir = Path.Combine(domainDirectory, "uniqueSamples");
			if (useFullGenerator) uniqueSamplesDir = Path.Combine(domainDirectory, "uniqueSamplesFULL");
			int maxOccurences = 10;

			if (!Directory.Exists(uniqueSamplesDir))
				Directory.CreateDirectory(uniqueSamplesDir);

			foreach (var sampleCollectionDir in Directory.EnumerateDirectories(statesDir))
			{
				string outputDirectoryPath = Path.Combine(uniqueSamplesDir, Path.GetFileName(sampleCollectionDir));
				if (!Directory.Exists(outputDirectoryPath))
					Directory.CreateDirectory(outputDirectoryPath);

				string featuresFolder = Path.Combine(samplesDir, Path.GetFileName(sampleCollectionDir));

				int fileID = -1;
				foreach (var statesFile in Utils.IO.Directory.EnumerateFilesBySize(sampleCollectionDir))
				{
					fileID++;
					string outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileName(statesFile));
					if (File.Exists(outputFilePath))
					{
						Console.WriteLine("skipping file " + outputFilePath);
						continue;
					}

					if (fileID % totalWorkers != workerIndex)
					{
						Console.WriteLine("skipping file " + outputFilePath + " (left to another worker)");
						continue;
					}

					Console.WriteLine("Processing file " + statesFile + " at " + DateTime.Now);
					var featuresFile = Path.Combine(featuresFolder, Path.GetFileName(statesFile));


					using (var writter = new StreamWriter(outputFilePath))
					{
						HashSet<string> processedStates = new HashSet<string>();
						do
						{
							var states = File.ReadLines(statesFile);
							var notProcessedState = states.Where(s => !processedStates.Contains(s)).FirstOrDefault();
							if (notProcessedState == default)
								break;

							var features = File.ReadLines(featuresFile);
							var statesWithFeatures = states.Zip(features);
							var samples = statesWithFeatures.Where(s => s.Item1 == notProcessedState).Take(maxOccurences).ToList();

							int occurences = Math.Min(samples.Count(), maxOccurences);
							for (int i = 0; i < occurences; i++)
							{
								writter.WriteLine(samples.First().Item1 + "\t" + samples.First().Item2);
							}
							processedStates.Add(samples.First().Item1);

						}
						while (true);
					}
					Console.WriteLine("File " + statesFile + " processed at " + DateTime.Now);
				}
			}
		}

		public static void storeTrainingSamples(string domainName, int workerID, int totalWorkers, bool useFullGenerators = false)
		{
			string domainDirectory = @"B:\SAS_Data\" + domainName;
			string samplesDirName = "samples";
			if (useFullGenerators) samplesDirName = "samplesFULL";
			string samplesDir = Path.Combine(domainDirectory, samplesDirName);
			if (!Directory.Exists(samplesDir))
				Directory.CreateDirectory(samplesDir);
			string statesDir = Path.Combine(domainDirectory, "sampledStates");

			(Func<Microsoft.Msagl.Drawing.Node, int> labelingFunc, int labelSize) labelingData = default;

			string generatorsDir = Path.Combine(domainDirectory, "featuresGen");

			foreach (var sampleCollectionDir in Directory.EnumerateDirectories(statesDir))
			{
				string outputDirectoryPath = Path.Combine(samplesDir, Path.GetFileName(sampleCollectionDir));
				if (!Directory.Exists(outputDirectoryPath))
					Directory.CreateDirectory(outputDirectoryPath);

				int fileID = -1;
				foreach (var statesFile in Utils.IO.Directory.EnumerateFilesBySize(sampleCollectionDir))
				{
					fileID++;
					string outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileName(statesFile));
					if (File.Exists(outputFilePath))
					{
						Console.WriteLine("skipping file " + outputFilePath);
						continue;
					}
					if (fileID % totalWorkers != workerID)
						continue;
					Console.WriteLine("Processing file " + statesFile + " at " + DateTime.Now);
					FFHeuristic heurFF = null;
					List<int> generatedVectorSizes = null;

					List<GraphsFeaturesGenerator<IntLabeledGraph, GraphNode, int>> generators = Directory.EnumerateFiles(generatorsDir).
						Select(q => (GraphsFeaturesGenerator<IntLabeledGraph, GraphNode, int>)Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(q)).ToList();
					if (useFullGenerators)
						generators = Directory.EnumerateFiles(generatorsDir).Where(q => Path.GetFileNameWithoutExtension(q).Contains("FULL") &&
						!Path.GetExtension(q).Contains("meta")).Select(q => (GraphsFeaturesGenerator<IntLabeledGraph, GraphNode, int>)Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>.load(q)).ToList();
					using (var writter = new StreamWriter(outputFilePath))
					{
						foreach (var line in File.ReadLines(statesFile))
						{
							var stateDesc = line.Split("\t").First();
							var stateAndProblem = Helper.ReconstructState(stateDesc);
							var state = stateAndProblem.state;
							var problem = stateAndProblem.problem;

							problem.SetInitialState(state);
							var graph = KnowledgeExtractionGraphs.computeObjectGraph(problem).toMSAGLGraph();
							if (labelingData == default)
								labelingData = UtilsMethods.getLabelingFunction(graph);

							var myGraph = IntLabeledGraph.createFromMSAGLGraph(graph, labelingData.labelingFunc);

							List<double[]> features = null;

							if (!useFullGenerators)
							{
								if (generatedVectorSizes == null)
								{
									generatedVectorSizes = generators.Select(g => g.getFeatures(myGraph).Length).ToList();
								}
								var vec = generators[2].getFeatures(myGraph);
								features = new List<double[]>() { vec.Take(generatedVectorSizes[0]).ToArray(), vec.Take(generatedVectorSizes[1]).ToArray(), vec };
							}
							else
							{
								features = generators.Select(q => q.getFeatures(myGraph)).ToList();
							}

							if (heurFF == null)
								heurFF = new FFHeuristic(problem);

							var FF = heurFF.GetValue(state);
							features.Add(new double[] { FF });
							writter.WriteLine(string.Join("\t", features.Select(q => string.Join(",", q))));
						}
					}
					Console.WriteLine("File " + statesFile + " processed at " + DateTime.Now);
				}
			}

		}

		/// <summary>
		/// Generates a set of states together with goal distances that can be used as training data for ML model. States are strored in the domain folder / states / xx where "xx" is the name of the file.
		/// </summary>
		/// <param name="domainName"></param>
		/// <param name="outputFile"></param>
		/// <param name="domain"></param>
		public static void storeStatesForTraining(string domainName, DomainType domain)
		{
			var problems = PADD_Support.SupportMethods.LoadSASProblemsForDomain(domainName);
			foreach (var item in problems)
			{
				var statesFolderPrefix = @"B:\SAS_Data\";
				var directory = Path.GetFileName(Path.GetDirectoryName(item.GetInputFilePath()));
				var outputDir = Path.Combine(statesFolderPrefix, directory, "states");
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(item.GetInputFilePath()) + ".tsv");



				/*
				var directory = Path.GetDirectoryName(item.GetInputFilePath());
				var outputDir = Path.Combine(directory, "states");
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(item.GetInputFilePath()) + ".tsv");
				*/

				if (File.Exists(outputFile))
					continue;
				AStarSearchEnumerator.storeStatesAsTSV(outputFile, new List<Problem>() { item }, domain);
			}

		}

		/// <summary>
		/// Generates a set of states together with goal distances that can be used as training data for ML model. States are strored in the domain folder / states / xx where "xx" is the name of the file.
		/// </summary>
		/// <param name="domainName"></param>
		/// <param name="outputFile"></param>
		/// <param name="domain"></param>
		public static void storeBackwardsStatesForTraining(string domainName)
		{
			var problems = PADD_Support.SupportMethods.LoadSASProblemsForDomain(domainName);
			foreach (var item in problems)
			{
				var statesFolderPrefix = @"B:\SAS_Data\";
				var directory = Path.GetFileName(Path.GetDirectoryName(item.GetInputFilePath()));
				var outputDir = Path.Combine(statesFolderPrefix, directory, "states");
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(item.GetInputFilePath()) + "_Backwards.tsv");

				/*
				var directory = Path.GetDirectoryName(item.GetInputFilePath());
				var outputDir = Path.Combine(directory, "states");
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(item.GetInputFilePath()) + ".tsv");
				*/

				if (File.Exists(outputFile))
					continue;

				var states = StateSpaceEnumerator.enumerateAllStatesWithDistances(item, useRelativeStates: true);
				TimeSpan timeLimit = TimeSpan.FromMinutes(6000);

				using (var writter = new StreamWriter(outputFile))
				{
					System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
					Console.WriteLine("Enumerating states for file " + item);
					foreach (var s in states)
					{
						if (watch.Elapsed > timeLimit)
						{
							Console.WriteLine("Time's up (at " + DateTime.Now + ")");
							break;
						}
						writter.WriteLine((s.state).GetInfoString(item) + "\t" + s.realDistance);
					}
				}
			}
		}

		/// <summary>
		/// Goes through previously stored states and filters out duplicities. Unique states are stored in a folder "uniqueStates". If a state is encountered several times, it is stored with a value that is a minimum of values found.
		/// </summary>
		/// <param name="domainName"></param>
		public static void storeUniqueStatesForTraining(string domainName)
		{
			var problems = PADD_Support.SupportMethods.LoadSASProblemsForDomain(domainName);
			foreach (var item in problems)
			{
				var statesFolderPrefix = @"B:\SAS_Data\";
				var directory = Path.GetFileName(Path.GetDirectoryName(item.GetInputFilePath()));
				var inputDir = Path.Combine(statesFolderPrefix, directory, "states");
				var outputDir = Path.Combine(statesFolderPrefix, directory, "uniqueStates");
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				var problemName = Path.GetFileNameWithoutExtension(item.GetInputFilePath());
				var outputFile = Path.Combine(outputDir, problemName + ".tsv");
				if (File.Exists(outputFile))
					continue;

				var uniqueStates = new Utils.Datastructures.Trie<char, int>();
				//var uniqueStates = new Dictionary<string, int>();

				string prefix = directory + "_" + item.GetProblemName() + ".sas_" + "[",
					suffix = " ]";

				Console.WriteLine("creating file " + outputFile);
				foreach (var file in Directory.EnumerateFiles(inputDir).Where(f => problemName == Path.GetFileNameWithoutExtension(f) || Path.GetFileName(f).StartsWith(problemName + "_")))
				{
					Console.WriteLine("\tprocessing file " + file + "\tstarted at " + DateTime.Now);

					foreach (var line in File.ReadLines(file))
					{
						var splitted = line.Split('\t');
						if (!splitted.First().StartsWith(prefix) || !splitted.First().EndsWith(suffix))
						{
							try
							{
								var realPrefix = splitted.First().Substring(0, prefix.Length);
								var realSuffix = splitted.First().Substring(splitted.First().Length - suffix.Length);
							}
							catch { }
							Console.WriteLine("\t\tcannot parse line " + line);
							continue;
						}

						string keyChars = splitted.First().Remove(0, prefix.Length);
						keyChars = keyChars.Remove(keyChars.Length - suffix.Length);
						int val = int.Parse(splitted.Last());
						uniqueStates.AddModify(keyChars.ToCharArray(), (a, b) => Math.Min(a, b), val);
						//uniqueStates.AddModify(keyChars, val, (a, b) => Math.Min(a, b));
					}
				}

				if (uniqueStates.Count == 0)
					continue;

				using (var writter = new StreamWriter(outputFile))
				{
					foreach (var s in uniqueStates.EnumerateAllValues())
					{
						writter.WriteLine(prefix + string.Join("", s.key) + suffix + "\t" + s.value);
						//writter.WriteLine(prefix + s.Key + suffix + "\t" + s.Value);
					}
				}
			}
		}

		/// <summary>
		/// Trains feature generator on previously generated states
		/// </summary>
		/// <param name="domainName"></param>
		/// <param name="subgraphSizes"></param>
		public static void trainAndSaveFeaturesGenerator(string domainName, List<int> subgraphSizes)
		{
			var outputDirectory = @"B:\SAS_Data\" + domainName + @"\featuresGen";
			if (!Directory.Exists(outputDirectory))
				Directory.CreateDirectory(outputDirectory);

			var sampledGraphs = sampleGraphs(domainName);

			foreach (var item in subgraphSizes)
			{
				Console.WriteLine("processing size " + item);

				var outputFileName = "generator_" + item + ".bin";
				var outputFilePath = Path.Combine(outputDirectory, outputFileName);

				if (File.Exists(outputFilePath))
				{
					Console.WriteLine("File " + outputFilePath + " already exists.");
					continue;
				}

				Console.WriteLine("creating file " + outputFileName + " at " + DateTime.Now);

				Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int> g = new Subgraphs_FeaturesGenerator<IntLabeledGraph, GraphNode, int>();
				g.train(sampledGraphs, item);
				g.save(outputFilePath);

			}
		}

		public static Heuristic getHeuristicByParam(int param, Problem d)
		{
			Heuristic h = null;

			//Heuristic innerHeuristic = new FFHeuristic(d);
			Heuristic innerHeuristic = new WeightedHeuristic(new FFHeuristic(d), 2);

			switch (param)
			{
				case 1:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.05));
					break;
				case 2:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.1));
					break;
				case 3:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.15));
					break;
				case 4:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.2));
					break;
				case 5:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.3));
					break;
				case 6:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.4));
					break;
				case 7:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.05));
					break;
				case 8:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.1));
					break;
				case 9:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.15));
					break;
				case 10:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.2));
					break;
				case 11:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.3));
					break;
				case 12:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.4));
					break;
				case 13:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 1));
					break;
				case 14:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 2));
					break;
				case 15:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 3));
					break;
				case 16:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 4));
					break;
				case 17:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 5));
					break;
				case 18:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 1));
					break;
				case 19:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 2));
					break;
				case 20:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 3));
					break;
				case 21:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 4));
					break;
				case 22:
					h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 5));
					break;
				default:
					break;
			}
			return h;
		}

		/// <summary>
		/// Samples states from previously generated states
		/// </summary>
		/// <param name="domainName"></param>
		/// <returns></returns>
		public static List<IntLabeledGraph> sampleGraphs(string domainName)
		{
			List<IntLabeledGraph> res = new List<IntLabeledGraph>();
			string statesFolder = Path.Combine(@"B:\SAS_Data\", domainName, "uniqueStates");

			int samplesPerFile = 1000;
			Random r = new Random(123);
			List<string> selectedLines = new List<string>();

			foreach (var item in Directory.EnumerateFiles(statesFolder))//.Skip(20).Take(20))
			{
				Console.WriteLine("sampling file " + item + " at " + DateTime.Now);
				var lines = File.ReadAllLines(item);
				if (lines.Length < samplesPerFile)
					selectedLines.AddRange(lines);
				else
				{
					for (int i = 0; i < samplesPerFile; i++)
					{
						selectedLines.Add(lines[r.Next(lines.Length)]);
					}
				}
			}
			Console.WriteLine("parsing states at " + DateTime.Now);
			var statesWithProblems = selectedLines.Select(q => Helper.ReconstructState(q.Split("\t").First())).ToList();
			selectedLines.Clear();
			(Func<Microsoft.Msagl.Drawing.Node, int> labelingFunc, int labelSize) labelingData = default;
			Console.WriteLine("Computing graphs at " + DateTime.Now);
			long completed = 1;
			foreach (var item in statesWithProblems)
			{
				completed++;
				if (completed % 1000 == 0)
				{
					Console.WriteLine("completed " + completed + " out of " + statesWithProblems.Count + " (" + (completed * 100 / statesWithProblems.Count) + " %) at " + DateTime.Now);
				}
				var problem = item.problem;
				problem.SetInitialState(item.state);
				var graph = KnowledgeExtractionGraphs.computeObjectGraph(problem).toMSAGLGraph();
				if (labelingData == default)
					labelingData = UtilsMethods.getLabelingFunction(graph);
				res.Add(IntLabeledGraph.createFromMSAGLGraph(graph, labelingData.labelingFunc));
			}

			return res;
		}

		public static void testNeuralNetHeuristic(string sasFilePath, string storedNetPath, string generatorPath, int maxTime, string domain, bool storeSamples)
		{
			bool useFFasFeature = true;
			Problem p = new Problem(sasFilePath, false);

			DomainDependentSolver domainSolver = null;
			switch (domain)
			{
				case "zenotravel":
					domainSolver = new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver();
					break;
				case "blocks":
					domainSolver = new PADD.DomainDependentSolvers.BlocksWorld.BlocksWorldSolver();
					break;
				default:
					throw new ArgumentException();
			}

			SimpleFFNetHeuristic h = storeSamples ?
				new SimpleFFNetHeuristic(generatorPath, storedNetPath, p, useFFasFeature, TargetTransformationType.none, domainSolver, generatorPath) :
				new SimpleFFNetHeuristic(generatorPath, storedNetPath, p, useFFasFeature, TargetTransformationType.none);

			var result = PADD_Support.SupportMethods.runPlanner(sasFilePath, h, maxTimeMinutes: maxTime);

			File.AppendAllLines("results.txt", new[] { result.ToString() });
			if (storeSamples)
			{
				var newSamples = h.newSamples;
				//h.newSamples = Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>("additionalSamples_17.Sep1832200PM.bin");
				string newSamplesFileName = "additionalSamples_.tsv";
				File.WriteAllLines(newSamplesFileName, h.newSamples.Select(q => string.Join(",", q.Item1.inputs) + "\t" + q.Item1.targets.Single() + "\t" + q.output));
			}
		}

		public static void iterativeLearning(int iteration, int maxMinutes, bool isBlocks = false)
		{
			Console.WriteLine($"Search iteration {iteration} started at " + DateTime.Now);
			int limit = isBlocks ? 27 : 20;

			for (int i = 1; i <= limit; i++)
			{
				testFFNetHeuristic(i, HeuristicType.net, iteration, maxMinutes, true, isBlocks: isBlocks);
			}
		}


		public static void testFFNetHeuristic(int fileNumber, HeuristicType type, int filePrefix = 1, int MaxTime_Minutes = 1, bool storeSamples = false, bool isBlocks = false)
		{
			string problem = "pfile" + fileNumber + ".sas";

			if (isBlocks)
				problem = "probBLOCKS-" + (fileNumber + 3) + "-0.sas";

			string state = "[1 0 2 0 3 2x4 3 0 5 1 3";

			//string samplesFolder = Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "trainingSamples");
			string samplesFolder = @"B:\iterativeTraining";
			//string samplesFolder = @"B:\trainingSamplesLarge2";

			if (isBlocks)
				samplesFolder = @"B:\iterativeTraining Blocks";
			//string samplesFolder = @"B:\trainingSamplesLarge2";

			int subgraphSize = 4;
			NormalizationType normalization = NormalizationType.Covariance;
			TargetTransformationType targeTransformation = TargetTransformationType.SqrtLog;
			bool useFFasFeature = true;
			Problem p = isBlocks ? 	new Problem(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, "blocksSmall", problem), false) :
									new Problem(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, "zenotravel", problem), false);

			//SASState s = SASState.parse(state, p);
			string subFolder = subgraphSize.ToString() + (useFFasFeature ? "F" : "") + NormalizationTypeHelper.ToChar(normalization) +
				TargetTransformationTypeHelper.ToChar(targeTransformation);

			string generatorsPath = Path.Combine(samplesFolder, subFolder, "graphFeaturesGen_Generator.bin");
			//string generatorsPath = Path.Combine(samplesFolder, subFolder, "trainedGeneratorNEW.bin");
			string savedNetPath = Path.Combine(samplesFolder, subFolder, "trainedNet_params.bin");

			DomainDependentSolver domainSolver = new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver();
			if (isBlocks)
				domainSolver = new PADD.DomainDependentSolvers.BlocksWorld.BlocksWorldSolver();
			SimpleFFNetHeuristic h = storeSamples ?
				new SimpleFFNetHeuristic(generatorsPath, savedNetPath, p, useFFasFeature, targeTransformation, domainSolver, generatorsPath) :
				new SimpleFFNetHeuristic(generatorsPath, savedNetPath, p, useFFasFeature, targeTransformation);
			//var value = h.getValue(s);	//only works for pfile8.sas For testing purposes, the value should be close to 10

			Heuristic heur = h;
			bool useTwoQueues = false;

			switch (type)
			{
				case HeuristicType.FF:
					heur = new FFHeuristic(p);
					break;
				case HeuristicType.net:
					break;
				case HeuristicType.sum:
					heur = new SumHeuristic(new List<Heuristic>() { h, new FFHeuristic(new Problem(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, "zenotravel", problem), false)) });
					break;
				case HeuristicType.max:
					heur = new MaxHeuristic(new List<Heuristic>() { h, new FFHeuristic(new Problem(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, "zenotravel", problem), false)) });
					break;
				case HeuristicType.min:
					heur = new MinHeuristic(new List<Heuristic>() { h, new FFHeuristic(new Problem(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, "zenotravel", problem), false)) });
					break;
				case HeuristicType.weighted10:
					heur = new WeightedHeuristic(h, 10);
					break;
				case HeuristicType.doubleList:
					useTwoQueues = true;
					break;
				default:
					break;
			}

			Console.WriteLine();
			string domain = "zenotravel";
			if (isBlocks)
				domain = "blocksSmall";
			var result = PADD_Support.SupportMethods.runPlanner(Path.Combine(PADD_Support.SupportMethods.SAS_all_WithoutAxioms, domain, problem), heur, useTwoQueues: useTwoQueues, MaxTime_Minutes);

			File.AppendAllLines("results.txt", new[] { result.ToString() });
			if (storeSamples)
			{
				var newSamples = h.newSamples;
				//h.newSamples = Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>("additionalSamples_17.Sep1832200PM.bin");
				string newSamplesFileName = "additionalSamples_" + filePrefix + "_" + fileNumber + "_" + (DateTime.Now.ToString() + ".bin").Replace(":", "").Replace(" ", "");
				Utils.Serialization.BinarySerialization.Serialize(newSamples, Path.Combine(samplesFolder, subFolder, "NewSamples", newSamplesFileName));
			}
			/*
			var currentNewSamples = new List<NeuralNetTrainer.TrainingSample>(h.newSamples);
			var netOutputs = currentNewSamples.Select(q => h.getValue(SASState.parse(q.userData.Split('_').Last(), p))).ToList();
			var targetOutputs = h.newSamples.Zip(netOutputs, (a, b) => a.targets.Single() + "\t" + b);
			File.WriteAllLines("newSamplesHH.tsv", targetOutputs);
			*/
		}



	}
}
