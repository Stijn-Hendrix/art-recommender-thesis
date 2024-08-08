using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages
{
	public class QuestionnaireModel : PageModel
	{
		public bool CanFillInQuestionnaire { get; set; } = false;

        public static string[] HoffmanLikert = new string[] {
            "The explanations help me understand how the recommendation system works.",
            "The explanations of the recommendations are satisfying.",
            "The explanations of the recommended artworks are sufficiently detailed.",
            "The explanations of the recommended artworks have all the information I need",
            "The explanations are actionable, that is, it helps me to know how to use the recommendation system.",
            "The explanations let me know how accurate or reliable the recommendation is.",
            "The explanations let me know how trustworthy the recommendation is."
        };

        public static string[] HoffmanOpenQuestions = new string[] {
            "Do you have any feedback for the explanations? Is there anything that you would change for the explanations?"
        };

        public static string[] AdditionalQuestionsForHoffman = new string[] {
             "The explanations highlights the characteristics that were essential for the recommendation. That is to say, if these characteristics change, the recommendation would also change.",
             "The explanations of the recommendations is inspecific. The explanation can be the same or similar for many other recommendations.",
            "The explanations are displayed in such a way that I understand what the most important information is that led to a recommendation.",
            "The recommender helped me discover surprisingly interesting paintings I might not have known otherwise." // http://arxiv.org/pdf/2008.02687
		};

        AppDbContext _artworkContext;

		public QuestionnaireModel(AppDbContext artworkContext)
		{
			_artworkContext = artworkContext;
		}

		public void OnGet()
		{
			UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);

			CanFillInQuestionnaire = user.StudyFinished(_artworkContext);
		}
	}
}
