using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_tool
{
	public class Program
	{
		static void Main(string[] args)
		{
			int problemID = 3947;
			List<string> plan = new List<string>()
			{
				"(unstack g l)",
"(put-down g)",
"(unstack o m)",
"(put-down o)",
"(unstack l f)",
"(put-down l)",
"(unstack f c)",
"(put-down f)",
"(unstack c n)",
"(put-down c)",
"(unstack n e)",
"(put-down n)",
"(unstack e h)",
"(put-down e)",
"(unstack h d)",
"(put-down h)",
"(pick-up l)",
"(stack l h)",
"(unstack d a)",
"(put-down d)",
"(unstack a k)",
"(put-down a)",
"(unstack k b)",
"(put-down k)",
"(unstack b j)",
"(put-down b)",
"(unstack j i)",
"(put-down j)",
"(pick-up i)",
"(stack i l)",
"(pick-up o)",
"(stack o i)",
"(pick-up n)",
"(stack n o)",
"(pick-up c)",
"(stack c n)",
"(pick-up b)",
"(stack b c)",
"(pick-up a)",
"(stack a b)",
"(pick-up m)",
"(stack m a)",
"(pick-up e)",
"(stack e m)",
"(pick-up j)",
"(stack j e)",
"(pick-up k)",
"(stack k j)",
"(pick-up f)",
"(stack f k)",
"(pick-up g)",
"(stack g f)",
"(pick-up d)",
"(put-down d)",
"(pick-up d)",
"(stack d g)"
			};

			var response = PlanSubmission.submitPlan(plan, problemID);

			Console.WriteLine(response);
		}
	}
}
