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

		private static string[] delimiters = new string[] { "(", ",", " ", ")" };
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
						if (parts.Contains("person"))
						{
							int id = int.Parse(item.Substring("person".Length));
							if (!personsByIDs.ContainsKey(id))
								personsByIDs.Add(id, new Person(id));
							if (value == currentValue)
							{
								var person = personsByIDs[id];
								if (parts[1].Contains("city"))
								{
									person.location = int.Parse(parts[1].Substring("city".Length));
									person.isBoarded = false;
								}
								if (parts[1].Contains("plane"))
								{
									person.location = int.Parse(parts[1].Substring("plane".Length));
									person.isBoarded = true;
								}
							}
							if (value == desiredValue)
							{
								var person = personsByIDs[id];
								if (parts[1].Contains("city"))
								{
									person.destination = int.Parse(parts[1].Substring("city".Length));
								}
								if (parts[1].Contains("plane"))
								{
									throw new Exception("Plane cannot be person's destination");
								}
							}
						}
						if (parts.Contains("city"))
						{
							int id = int.Parse(item.Substring("city".Length));
							if (!cities.Contains(id))
								cities.Add(id);
						}
						if (parts.Contains("plane"))
						{
							int id = int.Parse(item.Substring("plane".Length));
							if (!planesByIDs.ContainsKey(id))
								planesByIDs.Add(id, new Plane(id));
							if (value == currentValue)
							{
								if (meaning.Contains("at(plane"))
								{
									var plane = planesByIDs[id];
									plane.location = int.Parse(parts[1].Substring("city".Length));
								}
								if (meaning.Contains("fuel-level(plane"))
								{
									var plane = planesByIDs[id];
									plane.fuelReserve = int.Parse(parts[1].Substring("fl".Length));
								}
							}
							if (value == desiredValue)
							{
								throw new Exception("Plane's fuel reserve cannot be among goal conditions.");
							}
						}
					}
				}
			}
		}

		public static ZenoTravelProblem loadFromSAS(SASProblem zenoTravelProblemInSAS)
		{
			ZenoTravelProblem res = new ZenoTravelProblem();
			res.load(zenoTravelProblemInSAS);
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
		public int location,
			fuelReserve,
			ID;

		public Plane(int ID)
		{
			this.ID = ID;
		}

	}

	class Person
	{
		public int ID,
			location,
			destination;

		public bool isBoarded;

		public Person(int ID)
		{
			this.ID = ID;
		}
	}

}
