using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support
{
	public static class PlanSubmission
	{
		private static readonly HttpClient client = new HttpClient();

		public static string submitPlan(List<string> plan, int problemID)
		{
			var URL = "http://api.planning.domains";
			string queryURL = "/classical/submitplan/" + problemID;
			client.DefaultRequestHeaders.TryAddWithoutValidation("Content-type", "application/x-www-form-urlencoded");
			client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/plain");

			var values = new Dictionary<string, string>
			{
			   { "plan", string.Join("\n", plan)},
			   { "email", "OtaTrunda@gmail.com" }
			};

			var content = new FormUrlEncodedContent(values);

			var response = client.PostAsync(URL + queryURL, content).Result;

			var responseString = response.Content.ReadAsStringAsync().Result;

			return responseString;
		}
	}
}
