using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	class ZenoTravelProblem
	{
		public HashSet<int> cities;
		public Dictionary<int, Plane> planesByIDs;
		public Dictionary<int, Person> personsByIDs;

		private static string[] delimiters = new string[] { "(", ",", " ", ")", "Atom" };
		private static Func<string, List<string>> splitSAS = new Func<string, List<string>>(f => f.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).ToList());

		private void load(SASProblem zenoTravelProblemInSAS)
		{
			SASState initialState = (SASState)zenoTravelProblemInSAS.GetInitialState();
			int variableIndex = -1;
			foreach (var variable in zenoTravelProblemInSAS.variablesData)
			{
				variableIndex++;
				int currentValue = initialState.GetValue(variableIndex);
				int desiredValue = -1;
				if (zenoTravelProblemInSAS.GetGoalConditions().Any(g => g.variable == variableIndex))
					desiredValue = zenoTravelProblemInSAS.GetGoalConditions().Where(g => g.variable == variableIndex).Single().value;
				int value = -1;

				foreach (var meaning in variable.valuesSymbolicMeaning)
				{
					value++;
					var parts = splitSAS(meaning);
					foreach (var item in parts)
					{
						if (item.Contains("person"))
						{
							int id = int.Parse(item.Substring("person".Length));
							if (!personsByIDs.ContainsKey(id))
								personsByIDs.Add(id, new Person(id));
							if (value == currentValue)
							{
								var person = personsByIDs[id];
								if (parts[2].Contains("city"))
								{
									person.location = int.Parse(parts[2].Substring("city".Length));
									person.isBoarded = false;
								}
								if (parts[2].Contains("plane"))
								{
									person.location = int.Parse(parts[2].Substring("plane".Length));
									person.isBoarded = true;
								}
							}
							if (value == desiredValue)
							{
								var person = personsByIDs[id];
								if (parts[2].Contains("city"))
								{
									person.destination = int.Parse(parts[2].Substring("city".Length));
								}
								if (parts[2].Contains("plane"))
								{
									throw new Exception("Plane cannot be person's destination");
								}
							}
						}
						if (item.Contains("city"))
						{
							int id = int.Parse(item.Substring("city".Length));
							if (!cities.Contains(id))
								cities.Add(id);
						}
						if (item.Contains("plane"))
						{
							int id = int.Parse(item.Substring("plane".Length));
							if (!planesByIDs.ContainsKey(id))
								planesByIDs.Add(id, new Plane(id));
							if (value == currentValue)
							{
								if (meaning.Contains("at(plane"))
								{
									var plane = planesByIDs[id];
									plane.location = int.Parse(parts[2].Substring("city".Length));
								}
								if (meaning.Contains("fuel-level(plane"))
								{
									var plane = planesByIDs[id];
									plane.fuelReserve = int.Parse(parts[2].Substring("fl".Length));
								}
							}
							if (value == desiredValue)
							{
								if (meaning.Contains("at(plane"))
								{
									var plane = planesByIDs[id];
									plane.destination = int.Parse(parts[2].Substring("city".Length));
								}
								if (meaning.Contains("fuel-level(plane"))
								{
									throw new Exception("Plain's fuel reserve cannot be among goal conditions.");
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Removes all persons that are already in their destinations, or persons that don't have any destination set.
		/// Then it groups remaining persons by their locations and destnations, and groups of persons travelling the same route are replaced by a single representative with larger weight
		/// </summary>
		private void preprocess()
		{
			var toRemove = personsByIDs.Keys.Where(k => personsByIDs[k].location == personsByIDs[k].destination || personsByIDs[k].destination == -1).ToList();
			foreach (var item in toRemove)
				personsByIDs.Remove(item);
			
			toRemove.Clear();
			var groups = personsByIDs.Values.GroupBy(person => (person.location, person.destination)).Where(group => group.Count() > 1).Select(g => g.ToList());
			foreach (var item in groups)
			{
				Person representatve = item.First();
				representatve.weight = item.Count;
				foreach (var represented in item.Skip(1))
				{
					toRemove.Add(represented.ID);
				}
			}
			foreach (var item in toRemove)
				personsByIDs.Remove(item);
		}

		public static ZenoTravelProblem loadFromSAS(SASProblem zenoTravelProblemInSAS)
		{
			ZenoTravelProblem res = new ZenoTravelProblem();
			res.load(zenoTravelProblemInSAS);
			res.preprocess();
			return res;
		}

		protected ZenoTravelProblem()
		{
			cities = new HashSet<int>();
			planesByIDs = new Dictionary<int, Plane>();
			personsByIDs = new Dictionary<int, Person>();
		}
	}

	class Plane
	{
		public int location = -1,
			destination = -1,
			fuelReserve = -1,
			ID = -1;

		public Plane(int ID)
		{
			this.ID = ID;
		}

		public bool isDestinationSet => destination != -1;
	}

	class Person
	{
		public int ID = -1,
			location = -1,
			destination = -1;

		public bool isBoarded;

		public Person(int ID)
		{
			this.ID = ID;
			this.weight = 1;
		}

		public bool isDestinationSet => this.destination != -1;

		/// <summary>
		/// When there are more several persons that have the same location and destination, we represent them by a single person that has greater weight
		/// </summary>
		public int weight;
	}

}
