using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages
{
    public class SurveyCategoryExplanationsModel : PageModel
    {


        AppDbContext _artworkContext;

        public bool CanFillInQuestionnaire { get; private set; } = false;

        public SurveyCategoryExplanationsModel(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public void OnGet()
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            var type = user.CurrentExplanationType();
			CanFillInQuestionnaire = user.Recommendations.Where(e => e.ExplanationType == type).All(e => e.RecommendationRating != 0 && !e.CategoriesRelevantcyRatings.Contains((int)CategoryRating.UNSET));
        }
    }
}
