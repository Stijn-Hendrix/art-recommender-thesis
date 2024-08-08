using frontend.Models;
using frontend.Pages;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{

	public class QuestionResponse
	{
		public string Question { get; set; }
		public string Option { get; set; }
	}

	[Route("api/survey")]
	[ApiController]
	public class SurveyController : Controller
	{
		static AppDbContext _artworkContext;

		public SurveyController(AppDbContext artworkContext)
		{
			_artworkContext = artworkContext;
		}


		[HttpPost]
		public IActionResult SubmitLikert([FromForm]  List<QuestionResponse> responses)
		{
			var user = UserController.GetCurrentUser(_artworkContext, HttpContext);
			var type = user.CurrentExplanationType();
			bool canFillInQuestionnaire = user.Recommendations.Where(e => e.ExplanationType == type).All(e => e.RecommendationRating != 0);
			if (!canFillInQuestionnaire)
			{
				return Redirect("/Recommendations");
			}

			var questResponses = Newtonsoft.Json.JsonConvert.SerializeObject(responses);
			var current = user.CurrentExplanationType();
			foreach (var rec in user.Recommendations)
			{
				if(rec.ExplanationType == current)
				{
					rec.QuestionnaireResponses = questResponses;
				}
			}

            _artworkContext.SaveChanges();

			if (user.Recommendations.Any(e =>  e.QuestionnaireResponses == ""))
			{
				return Redirect("/Recommendations"); 
			}
			else
			{
				UserController.FinishCurrentUser(_artworkContext, HttpContext);
                return Redirect("/ThankYouPage"); 
			}
		}
	}
}
