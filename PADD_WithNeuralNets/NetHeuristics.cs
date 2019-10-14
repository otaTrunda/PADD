using NeuralNetSpecificUtils;
using NeuralNetSpecificUtils.GraphFeatureGeneration;
using NeuralNetSpecificUtils.Graphs;
using NeuralNetTrainer;
using PADD;
using PADD.DomainDependentSolvers;
using PADD_Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.ExtensionMethods;
using Utils.MachineLearning;

namespace PADD_WithNeuralNets
{
	#region Learning heuristics

	abstract class LearningHeuristic<T> : Heuristic
	{
		protected Dictionary<T, int> trainingSamples;

		public virtual void addTrainingSamples(List<T> samples)
		{
			foreach (var item in samples)
			{
				if (!trainingSamples.ContainsKey(item))
					trainingSamples.Add(item, 0);
				trainingSamples[item]++;
			}
		}

		/// <summary>
		/// Method has to be called before the heuristic is used. After this method is called, no more training samples can be added.
		/// </summary>
		public abstract void train();

	}

	class HistogramBasedTrainingSample
	{
		/// <summary>
		/// real distance = shosrtest path to the goal from the state. HeuristicValue = value of the heuristic for the state, 
		/// variablesCount = number of variables in the state (we assume that training samples may come from different problems).
		/// </summary>
		public int realDistance, heuristicValue, variablesCount;

		public HistogramBasedTrainingSample(int realDistance, int heuristicValue, int variablesCount)
		{
			this.realDistance = realDistance;
			this.heuristicValue = heuristicValue;
			this.variablesCount = variablesCount;
		}

		public HistogramBasedTrainingSample(int realDistance, int heuristicValue)
		{
			this.realDistance = realDistance;
			this.heuristicValue = heuristicValue;
		}

		public override bool Equals(object obj)
		{
			HistogramBasedTrainingSample other = (HistogramBasedTrainingSample)obj;
			return this.heuristicValue == other.heuristicValue && this.realDistance == other.realDistance && this.variablesCount == other.variablesCount;
		}

		public override int GetHashCode()
		{
			return ((this.heuristicValue * 1000) + this.variablesCount * 1000) + this.realDistance;
		}
	}
	/*
	abstract class HistogramBasedLearningHeuristic : LearningHeuristic<HistogramBasedTrainingSample>
	{
		public static Dictionary<HistogramBasedTrainingSample, int> createTrainingSamples(List<HistogramVisualizer.Histograms> histograms)
		{
			//histograms are indexed by REAL goal value and contain dictionary of (heuristic estimate, count)
			Dictionary<HistogramBasedTrainingSample, int> result = new Dictionary<HistogramBasedTrainingSample, int>();
			foreach (var hist in histograms)
			{
				SASProblem p = SASProblem.CreateFromFile(@"./../tests/benchmarksSAS_ALL/" + hist.domain + "/" + hist.problem + ".sas");
				var item = hist.histograms;
				foreach (var realValue in item.Keys)
				{
					foreach (var heurValue in item[realValue].Keys)
					{
						var sample = new HistogramBasedTrainingSample(realValue, heurValue, p.GetVariablesCount());
						if (!result.ContainsKey(sample))
							result.Add(sample, 0);
						result[sample] += item[realValue][heurValue];
					}
				}
			}
			return result;
		}
	}

	class RegresionLearningHeuristic : LearningHeuristic<HistogramBasedTrainingSample>
	{
		/// <summary>
		/// The regression model represents the dependency of coefficient "c" on heuristic value and other parameters. The coefficient is computed as (realValue / heuristicValue) 
		/// and it is used as a multiplicator. By multiplying the heuristic value by "c" we should be close to real value.
		/// </summary>
		protected RegressionModel m;
		protected Heuristic baseHeuristic;
		protected int polynomialDegree = 3;
		protected double[] args;

		public override string getDescription()
		{
			return "Regresion heuristic";
		}

		public override void train()
		{
			args = new double[] { 0, 0 };
			this.m = trainRegressionModel(this.trainingSamples);
		}

		protected override double evaluate(IState state)
		{
			double heurVal = baseHeuristic.getValue(state);
			args[0] = heurVal;
			args[1] = this.problem.GetVariablesCount();
			return m.eval(args) * heurVal;
		}

		protected RegressionModel trainRegressionModel(Dictionary<HistogramBasedTrainingSample, int> trainingSamples)
		{
			Console.WriteLine("Heur\tVariables\tReal");
			foreach (var item in trainingSamples.Keys)
			{
				Console.WriteLine(item.heuristicValue + "," + item.variablesCount + "," + item.realDistance);
			}

			return new MultiPolynomialRegressionModel(this.polynomialDegree);

			List<double[]> trainingInputs = new List<double[]>();
			List<double> trainingResults = new List<double>();

			var problemSizes = trainingSamples.Keys.Select(t => t.variablesCount).Distinct();
			foreach (var size in problemSizes)
			{
				var relevantItems = trainingSamples.Keys.Where(t => t.variablesCount == size);
				var heurValues = relevantItems.Select(t => t.heuristicValue).Distinct();
				foreach (var heurVal in heurValues)
				{
					var items = relevantItems.Where(t => t.heuristicValue == heurVal);
					double mean = items.Average(t => t.realDistance);
					double trainResult = heurVal != 0 ? mean / heurVal : 1d;
					double[] trainInputs = new double[] { heurVal, size };
					trainingInputs.Add(trainInputs);
					trainingResults.Add(trainResult);
				}
			}

			RegressionModel m = new MultiPolynomialRegressionModel(this.polynomialDegree);
			m.trainModel(trainingInputs, trainingResults);
			return m;
		}

		public RegresionLearningHeuristic(Dictionary<HistogramBasedTrainingSample, int> samples, Heuristic baseHeuristic, SASProblem problem)
		{
			this.problem = problem;
			this.trainingSamples = samples;
			this.baseHeuristic = baseHeuristic;
		}
	}

	abstract class RegressionModel
	{
		protected Func<double[], double> evaluationFunction;
		protected Func<double, double> evaluationFunction2;

		public virtual double eval(double[] inputs)
		{
			return evaluationFunction(inputs);
		}

		public abstract void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets);
	}

	class MultiLinearRegressionModel : RegressionModel
	{
		public override void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets)
		{
			this.evaluationFunction = Fit.MultiDimFunc(batchOfInputs.ToArray(), batchOfTargets.ToArray(), true);
		}
	}

	class MultiPolynomialRegressionModel : RegressionModel
	{
		int degree;
		List<Func<double[], double>> kernelFunctions;

		public override void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets)
		{
			//createKernels(batchOfInputs.First().Length);
			//this.evaluationFunction = Fit.LinearMultiDimFunc(batchOfInputs.ToArray(), batchOfTargets.ToArray(), kernelFunctions.ToArray());
			//var p = Fit.LinearMultiDim(batchOfInputs.ToArray(), batchOfTargets.ToArray(), kernelFunctions.ToArray());
			this.evaluationFunction2 = Fit.PolynomialFunc(batchOfInputs.Select(t => t[0]).ToArray(), batchOfTargets.ToArray(), 2);
			this.evaluationFunction = (d => evaluationFunction2(d[0]));
			var p = Fit.Polynomial(batchOfInputs.Select(t => t[0]).ToArray(), batchOfTargets.ToArray(), 2);
		}

		protected virtual void createKernels(int numberOfvariables)
		{
			this.kernelFunctions = new List<Func<double[], double>>();
			kernelFunctions.AddRange(createPolynomialKernels(this.degree, numberOfvariables));
		}

		/// <summary>
		/// Creates all combiantions of functions in a form x_i * x_j * x_k * ... . E.g. for degree = 2 and numberOfVariables = 3 it will create a list of functions
		/// x_1 * x_1, x_1 * x_2, x_1 * x_3, x_2 * x_2, x_2 * x_3, x_3 * x_3
		/// </summary>
		/// <param name="degree"></param>
		/// <param name="numberOfVariables"></param>
		/// <returns></returns>
		protected List<Func<double[], double>> createPolynomialKernels(int degree, int numberOfVariables)
		{
			var res = new List<Func<double[], double>>();
			for (int i = 1; i <= degree; i++)
			{
				res.AddRange(createPolynomialKernelsRecur(0, numberOfVariables, i, new List<int>()).ToList());
			}
			res.Add(a => 1);
			return res;
		}

		private IEnumerable<Func<double[], double>> createPolynomialKernelsRecur(int currentVar, int maxVariables, int maxDegree, List<int> selectedIndices)
		{
			if (selectedIndices.Count == maxDegree)
			{
				yield return new Func<double[], double>(d =>
				{
					double result = 1;
					foreach (var index in selectedIndices)
					{
						result *= d[index];
					}
					return result;
				});
			}
			else
			{
				for (int i = currentVar; i < maxVariables; i++)
				{
					selectedIndices.Add(i);
					foreach (var item in createPolynomialKernelsRecur(i, maxVariables, maxDegree, selectedIndices))
					{
						yield return item;
					}
					selectedIndices.RemoveAt(selectedIndices.Count - 1);
				}
			}
		}

		public MultiPolynomialRegressionModel(int degree)
		{
			this.degree = degree;
		}
	}

	class RegHeuristic : Heuristic
	{
		private Heuristic h;
		private double[] polynomialCoefficients;

		public RegHeuristic(Heuristic h)
		{
			this.h = h;
			this.polynomialCoefficients = new double[] { 0.13361382, -4.66144875, 2.35220582 };
		}

		protected override double evaluate(IState state)
		{
			double heurVal = h.getValue(state);
			return heurVal * (polynomialCoefficients[2] * heurVal + polynomialCoefficients[1]) + polynomialCoefficients[0];
		}

		public override string getDescription()
		{
			return "Regression heuristic ";
		}
	}
	*/

	class NNHeuristic : Heuristic
	{
		public BrightWireNN network;
		FFHeuristic heur;
		List<float> nnInputs;
		double nextFFHeurResult = -1;

		public override string getDescription()
		{
			return "Neural net heuristic";
		}

		protected override double evaluate(IState state)
		{
			float heurVal = nextFFHeurResult == -1 ? (float)heur.getValue(state) : (float)nextFFHeurResult;
			nextFFHeurResult = -1;
			nnInputs[nnInputs.Count - 1] = heurVal;
			double networkVal = network.eval(nnInputs);
			return networkVal >= 0 ? networkVal + (double)heurVal / 10000 : heurVal;    //breaking ties in this heuristic values by values of the inner heuristic 
		}

		public NNHeuristic(SASProblem p)
		{
			this.heur = new FFHeuristic(p);
			var features = FeaturesCalculator.generateFeaturesFromProblem(p);
			features.Add(0);    //feature dependent on the state;
			nnInputs = features.Select(d => (float)d).ToList();
		}

		public NNHeuristic(SASProblem p, string trainedNetworkFile, bool useNetwork = true)
			: this(p)
		{
			if (useNetwork)
				this.network = BrightWireNN.load(trainedNetworkFile);
		}

		public override void sethFFValueForNextState(double heurVal)
		{
			this.nextFFHeurResult = heurVal;
		}
	}

	class SimpleFFNetHeuristic : Heuristic
	{
		IState originalState;
		Func<Microsoft.Msagl.Drawing.Node, float[]> labelingFunc;
		int labelSize;
		GraphsFeaturesGenerator gen, genForStoring;
		List<(float[,] weights, float[] biases)> netParams;
		DataNormalizer normalizer;
		public List<(TrainingSample, double output)> newSamples;

		Network net;
		DomainDependentSolver solver;
		bool storeStates = false;
		bool useFFHeuristicAsFeature = false;
		TargetTransformationType targetTransformation;
		FFHeuristic ffH;
		public Dictionary<IOperator, List<(List<float>, IState, MyLabeledGraph, float[], IState, MyLabeledGraph, float[])>> diffsByOps =
			new Dictionary<IOperator, List<(List<float>, IState, MyLabeledGraph, float[], IState, MyLabeledGraph, float[])>>();

		public Dictionary<IOperator, Dictionary<(int, List<int>), (int, Microsoft.Msagl.Drawing.Graph predGraph, Microsoft.Msagl.Drawing.Graph succGraph, float[] predecessorFeatures, string predMeaning, float[] successorFeatures, string succMeaning)>> newValsByPredecessorsVals =
			new Dictionary<IOperator, Dictionary<(int, List<int>), (int, Microsoft.Msagl.Drawing.Graph predGraph, Microsoft.Msagl.Drawing.Graph succGraph, float[] predecessorFeatures, string predMeaning, float[] successorFeatures, string succMeaning)>>();

		public Dictionary<IOperator, Utils.Datastructures.Trie<int, List<int>>> successorFeaturesByPredecessorFeaturesAndOperator = new Dictionary<IOperator, Utils.Datastructures.Trie<int, List<int>>>();

		private class TupleHashFunction : IEqualityComparer<(int, List<int>)>
		{
			private Utils.ListInt_EqualityComparer q;

			public bool Equals((int, List<int>) x, (int, List<int>) y)
			{
				if (x.Item1 != y.Item1)
					return false;
				return q.Equals(x.Item2, y.Item2);
			}

			public int GetHashCode((int, List<int>) obj)
			{
				return q.GetHashCode(obj.Item2) + obj.Item1 * 31;
			}

			public TupleHashFunction()
			{
				this.q = new Utils.ListInt_EqualityComparer();
			}
		}

		private bool storeStatistics(float[] predecessorFeatures, string predMeaning, float[] successorFeatures, string succMeaning, IOperator op, Microsoft.Msagl.Drawing.Graph predecessorGraph, Microsoft.Msagl.Drawing.Graph successorGraph)
		{
			if (!newValsByPredecessorsVals.ContainsKey(op))
				newValsByPredecessorsVals.Add(op, new Dictionary<(int, List<int>), (int, Microsoft.Msagl.Drawing.Graph, Microsoft.Msagl.Drawing.Graph, float[] predecessorFeatures, string predMeaning, float[] successorFeatures, string succMeaning)>(new TupleHashFunction()));
			for (int i = 0; i < predecessorFeatures.Length - 1; i++)
			{
				int previousVal = (int)predecessorFeatures[i];
				int newVal = (int)successorFeatures[i];
				List<int> determiningIndices = null;
				if (gen is SubgraphsSignatures_FeaturesGenerator)
				{
					determiningIndices = (gen as SubgraphsSignatures_FeaturesGenerator).indexesOfSubsets[i].OrderBy(x => x).ToList();
				}
				else determiningIndices = Enumerable.Range(0, predecessorFeatures.Length).ToList();
				var valsOfDeterminingIndices = predecessorFeatures.ElementsAt(determiningIndices).Select(x => (int)x).ToList();
				if (!newValsByPredecessorsVals[op].ContainsKey((i, valsOfDeterminingIndices)))
					newValsByPredecessorsVals[op].Add((i, valsOfDeterminingIndices), (newVal, predecessorGraph, successorGraph, predecessorFeatures, predMeaning, successorFeatures, succMeaning));
				bool isTheSame = newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].Item1 == newVal;
				if (!isTheSame)
				{
					Console.WriteLine("index " + i);
					Console.WriteLine("determining indices " + string.Join(" ", determiningIndices));
					Console.WriteLine("predecessor1Features " + string.Join(" ", newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].predecessorFeatures));
					Console.WriteLine("predecessor1 meaning: " + newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].predMeaning);
					Console.WriteLine("predecessor1Graph");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].predGraph);

					Console.WriteLine("succ1Features " + string.Join(" ", newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].successorFeatures));
					Console.WriteLine("succ1 meaning: " + newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].succMeaning);
					Console.WriteLine("succ1Graph");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(newValsByPredecessorsVals[op][(i, valsOfDeterminingIndices)].succGraph);


					Console.WriteLine("predecessor2Features " + string.Join(" ", predecessorFeatures));
					Console.WriteLine("predecessor1 meaning: " + predMeaning);
					Console.WriteLine("predecessor2Graph");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(predecessorGraph);

					Console.WriteLine("succ2Features " + string.Join(" ", successorFeatures));
					Console.WriteLine("succ2 meaning: " + succMeaning);
					Console.WriteLine("succ2Graph");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(successorGraph);

					return false;
				}
			}
			return true;
		}

		private bool storeStatistics2(float[] predecessorFeatures, string predMeaning, float[] successorFeatures, string succMeaning, IOperator op, Microsoft.Msagl.Drawing.Graph predecessorGraph, Microsoft.Msagl.Drawing.Graph successorGraph)
		{
			if (!successorFeaturesByPredecessorFeaturesAndOperator.ContainsKey(op))
			{
				successorFeaturesByPredecessorFeaturesAndOperator.Add(op, new Utils.Datastructures.Trie<int, List<int>>());
				successorFeaturesByPredecessorFeaturesAndOperator[op].Add(predecessorFeatures.Select(q => (int)q).ToList(), successorFeatures.Select(q => (int)q).ToList());
				return true;
			}
			if (!successorFeaturesByPredecessorFeaturesAndOperator[op].ContainsKey(predecessorFeatures.Select(q => (int)q).ToList()))
			{
				successorFeaturesByPredecessorFeaturesAndOperator[op].Add(predecessorFeatures.Select(q => (int)q).ToList(), successorFeatures.Select(q => (int)q).ToList());
				return true;
			}
			List<int> expectedFeatures = null;
			successorFeaturesByPredecessorFeaturesAndOperator[op].TryGetValue(predecessorFeatures.Select(q => (int)q).ToList(), out expectedFeatures);

			for (int i = 0; i < successorFeatures.Length; i++)
			{
				if ((int)successorFeatures[i] != expectedFeatures[i])
				{
					return false;
				}
			}
			return true;
		}

		public SimpleFFNetHeuristic(string featuresGeneratorPath, string savedNetworkPath, SASProblem problem, bool useFFHeuristicAsFeature, TargetTransformationType targeTransformation, bool useFullGenerator = false)
		{
			this.problem = problem;
			originalState = problem.GetInitialState();
			var labelingData = UtilsMethods.getLabelingFunction(KnowledgeExtraction.computeObjectGraph(problem).toMSAGLGraph());
			this.labelingFunc = labelingData.labelingFunc;
			this.labelSize = labelingData.labelSize;
			if (featuresGeneratorPath != null)
			{
				if (useFullGenerator)
					this.gen = Subgraphs_FeaturesGenerator.load(featuresGeneratorPath);
				else
					this.gen = SubgraphsSignatures_FeaturesGenerator.load(featuresGeneratorPath);
			}
			else
				this.gen = null;

			var parms = savedNetworkPath != null ? Network.loadParams(savedNetworkPath) : default;
			netParams = parms.Item1;
			normalizer = parms.Item2 != default ? DataNormalizer.LoadFromParams(parms.Item2) : null;
			this.useFFHeuristicAsFeature = useFFHeuristicAsFeature;
			//if (this.useFFHeuristicAsFeature)
			this.ffH = new FFHeuristic(problem);
			this.targetTransformation = targeTransformation;
			net = Network.load(savedNetworkPath);
		}

		/// <summary>
		/// If the solver is given, the heuristic will store all states on which it was evaluated as samples (it will store its features as well as target, for computing the target, the <paramref name="solver"/> will be used).
		/// </summary>
		/// <param name="featuresGeneratorPath"></param>
		/// <param name="savedNetworkPath"></param>
		/// <param name="problem"></param>
		/// <param name="solver"></param>
		public SimpleFFNetHeuristic(string featuresGeneratorPath, string savedNetworkPath, SASProblem problem, bool useFFHeuristicAsFeature, TargetTransformationType targetTransformation, PADD.DomainDependentSolvers.DomainDependentSolver solver,
			string generatorUsedForStoringStates, bool useFullGenerator = false) :
			this(featuresGeneratorPath, savedNetworkPath, problem, useFFHeuristicAsFeature, targetTransformation, useFullGenerator)
		{
			this.solver = solver;
			this.storeStates = true;
			this.newSamples = new List<(TrainingSample, double output)>();
			this.genForStoring = null;
			if (generatorUsedForStoringStates != null)
			{
				if (useFullGenerator)
					this.genForStoring = Subgraphs_FeaturesGenerator.load(generatorUsedForStoringStates);
				else
					this.genForStoring = SubgraphsSignatures_FeaturesGenerator.load(generatorUsedForStoringStates);

			}
		}

		public override string getDescription()
		{
			return "FF-net heuristic";
		}

		protected override double evaluate(IState state)
		{
			throw new NotImplementedException();
		}

		protected override double evaluate(IState state, IState predecessor, IOperator op)
		{
			this.originalState = problem.GetInitialState();
			problem.SetInitialState(state);

			var msaglGraph = KnowledgeExtraction.computeObjectGraph(problem);
			//PADDUtils.GraphVisualization.GraphVis.showGraph(msaglGraph.toMSAGLGraph());
			MyLabeledGraph graph = MyLabeledGraph.createFromMSAGLGraph(msaglGraph.toMSAGLGraph(), this.labelingFunc, this.labelSize);
			//var mmg = graph.toMSAGLGraph(true);
			//PADDUtils.GraphVisualization.GraphVis.showGraph(mmg);

			var features = getFeatures(state, graph);

			if (predecessor != null)
			{
				problem.SetInitialState(predecessor);
				var msaglGraphPred = KnowledgeExtraction.computeObjectGraph(problem);
				MyLabeledGraph graphPred = MyLabeledGraph.createFromMSAGLGraph(msaglGraphPred.toMSAGLGraph(), this.labelingFunc, this.labelSize);
				var predFeatures = getFeatures(predecessor, graphPred);

				storeStatistics(predFeatures, ((SASState)predecessor).toStringWithMeanings(), features, ((SASState)state).toStringWithMeanings(), op, graphPred.toMSAGLGraph(true), graph.toMSAGLGraph(true));

				var diff = features.Zip(predFeatures, (curr, pred) => curr - pred).ToList();
				if (!diffsByOps.ContainsKey(op))
					diffsByOps.Add(op, new List<(List<float>, IState, MyLabeledGraph, float[], IState, MyLabeledGraph, float[])>());

				if (diffsByOps[op].Any(x => !CollectionsExtenstensions.AreArraysEqual(x.Item1.ToArray(), diff.ToArray())))
				{
					Console.WriteLine("Operator: " + op.ToString());

					Console.WriteLine("Predecessor1:\t");
					Console.WriteLine(diffsByOps[op].First().Item2.ToString());
					int[] vals = ((SASState)diffsByOps[op].First().Item2).GetAllValues();
					for (int i = 0; i < vals.Length; i++)
					{
						Console.Write(problem.variablesData[i].GetValueSymbolicMeaning(vals[i]) + ", ");
					}
					Console.WriteLine();
					Console.WriteLine("Predecessor features:\t");
					Console.WriteLine(string.Join(" ", diffsByOps[op].First().Item4));
					Console.WriteLine("Predecessor graph:\t");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(diffsByOps[op].First().Item3.toMSAGLGraph());

					Console.WriteLine("Successor1:\t");
					Console.WriteLine(diffsByOps[op].First().Item5.ToString());
					vals = ((SASState)diffsByOps[op].First().Item5).GetAllValues();
					for (int i = 0; i < vals.Length; i++)
					{
						Console.Write(problem.variablesData[i].GetValueSymbolicMeaning(vals[i]) + ", ");
					}
					Console.WriteLine();
					Console.WriteLine("Sucessor features:\t");
					Console.WriteLine(string.Join(" ", diffsByOps[op].First().Item7));
					Console.WriteLine("Sucessor graph:\t");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(diffsByOps[op].First().Item6.toMSAGLGraph());
					Console.WriteLine("First diff:");
					Console.WriteLine(string.Join(" ", diffsByOps[op].First().Item1));

					Console.WriteLine("Predecessor2:\t");
					Console.WriteLine(predecessor.ToString());
					vals = ((SASState)predecessor).GetAllValues();
					for (int i = 0; i < vals.Length; i++)
					{
						Console.Write(problem.variablesData[i].GetValueSymbolicMeaning(vals[i]) + ", ");
					}
					Console.WriteLine();
					Console.WriteLine("Predecessor features:\t");
					Console.WriteLine(string.Join(" ", predFeatures));
					Console.WriteLine("Predecessor graph:\t");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(graphPred.toMSAGLGraph());

					Console.WriteLine("Successor2:\t");
					Console.WriteLine(state.ToString());
					vals = ((SASState)state).GetAllValues();
					for (int i = 0; i < vals.Length; i++)
					{
						Console.Write(problem.variablesData[i].GetValueSymbolicMeaning(vals[i]) + ", ");
					}
					Console.WriteLine();
					Console.WriteLine("Sucessor features:\t");
					Console.WriteLine(string.Join(" ", features));
					Console.WriteLine("Sucessor graph:\t");
					NeuralNetSpecificUtils.GraphVisualization.GraphVis.showGraph(graph.toMSAGLGraph());
					Console.WriteLine("Second diff:");
					Console.WriteLine(string.Join(" ", diff));
				}
				diffsByOps[op].Add((diff, predecessor, graphPred, predFeatures, state, graph, features));

			}

			TrainingSample s = null;
			if (storeStates)
			{
				solver.SetProblem(problem);
				var realGoalDistance = solver.Search(quiet: true);
				var targets = new double[] { realGoalDistance };
				var splitted = problem.GetInputFilePath().Split(System.IO.Path.DirectorySeparatorChar);
				string stateInfo = splitted[splitted.Length - 2] + "_" + splitted[splitted.Length - 1] + "_" + state.ToString();

				var inputs = genForStoring.getFeatures(graph);
				var hFF = ffH.getValue(state);
				s = new TrainingSample(inputs.Concat(hFF.Yield()).ToArray(), targets);
				s.userData = stateInfo;
			}

			//return 0;

			if (normalizer != null)
				features = normalizer.Transform(features.ToDoubles(), true).ToFloats();

			//var netOutput = netParams != default ? Network.executeByParams(netParams, features) : new float[] { 0 };
			var netOutput = net.evaluate(features.ToDoubles()).ToFloats();

			if (normalizer != null)
				netOutput = normalizer.ReverseTransform(netOutput.ToDoubles(), false).ToFloats();

			if (netParams != default)
				netOutput = TargetTransformationTypeHelper.ReverseTransform(netOutput.ToDoubles(), targetTransformation).ToFloats();

			if (storeStates)
			{
				newSamples.Add((s, netOutput.Single()));
			}

			problem.SetInitialState(originalState);
			return netOutput.Single();
		}

		protected float[] getFeatures(IState state, MyLabeledGraph graph)
		{
			var features = gen != null ? gen.getFeatures(graph).ToFloats() : new float[] { 0 };
			if (useFFHeuristicAsFeature)
			{
				var ffVal = ffH.getValue(state);
				features = features.Append((float)ffVal).ToArray();
			}
			return features;
		}
	}

	#endregion

	class FileBasedHeuristic : NNHeuristic
	{
		public FileBasedHeuristic(SASProblem p, string trainedNetworkFile, bool useNetwork = false) : base(p, trainedNetworkFile, useNetwork)
		{
			this.network = new FileBasedModel(trainedNetworkFile);
		}
	}

	public class FFNetHeuristicFactory
	{
		public string featuresGenPath, savedNetPath;
		bool useFFasFeature;
		TargetTransformationType transformation;
		DomainDependentSolver solver = null;
		string generatorUsedForStoringStates;

		public Heuristic create(SASProblem problem)
		{
			if (solver == null)
				return new SimpleFFNetHeuristic(featuresGenPath, savedNetPath, problem, useFFasFeature, transformation);
			return new SimpleFFNetHeuristic(featuresGenPath, savedNetPath, problem, useFFasFeature, transformation, solver, generatorUsedForStoringStates);
		}

		public FFNetHeuristicFactory(string featuresGeneratorPath, string savedNetworkPath, bool useFFHeuristicAsFeature, TargetTransformationType targeTransformation)
		{
			this.featuresGenPath = featuresGeneratorPath;
			this.savedNetPath = savedNetworkPath;
			this.useFFasFeature = useFFHeuristicAsFeature;
			this.transformation = targeTransformation;
		}

		public FFNetHeuristicFactory(string featuresGeneratorPath, string savedNetworkPath, bool useFFHeuristicAsFeature, TargetTransformationType targetTransformation, DomainDependentSolver solver,
			string generatorUsedForStoringStates) :
			this(featuresGeneratorPath, savedNetworkPath, useFFHeuristicAsFeature, targetTransformation)
		{
			this.solver = solver;
			this.generatorUsedForStoringStates = generatorUsedForStoringStates;
		}

	}

}
