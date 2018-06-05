using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace ProblemDownload
{
	/// <summary>
	/// Allows to download pddl problems from planning.domains. DomainID can be found at http://api.planning.domains/json/classical/domains
	/// </summary>
	class Downloader
	{
		public static void download(int domainID, string folderToDownloadTo)
		{
			string requestString = "http://api.planning.domains/json/classical/problems/" + domainID;
			WebClient c = new WebClient();
			string response = c.DownloadString(requestString);

			JsonResponse parsedResponse = JsonConvert.DeserializeObject<JsonResponse>(response);

			if (!Directory.Exists(folderToDownloadTo))
				Directory.CreateDirectory(folderToDownloadTo);
			string infoDirectory = Path.Combine(folderToDownloadTo, "_problemInfo");
			if (!Directory.Exists(infoDirectory))
				Directory.CreateDirectory(infoDirectory);

			c.DownloadFile(parsedResponse.result.First().domain_url, Path.Combine(folderToDownloadTo, "domain.pddl"));
			foreach (var item in parsedResponse.result)
			{
				c.DownloadFile(item.problem_url, Path.Combine(folderToDownloadTo, item.problem));
				using (var writter = new StreamWriter(Path.Combine(infoDirectory, Path.ChangeExtension(item.problem, ".txt")), append: true))
				{
					writter.WriteLine("lowerBound\t" + item.lower_bound);
					writter.WriteLine("upperBound\t" + item.upper_bound);
				}
			}
		}
	}

	class result
	{
		public int problem_id;
		public int domain_id;
		public string domain;
		public string problem;
		public string domain_url;
		public string problem_url;
		public string domain_path;
		public string problem_path;
		public string tags;
		public int? lower_bound;
		public int? upper_bound;
		public string average_effective_width;
		public string max_effective_width;
		public string lower_bound_description;
		public string upper_bound_description;
		public string average_effective_width_description;
		public string max_effective_width_description;
	}

	class JsonResponse
	{
		public bool error;
		public string message;
		public result[] result;
	}
}
