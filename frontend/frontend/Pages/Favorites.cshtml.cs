using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages
{
    public class FavoritesModel : PageModel
    {
        public List<string> ImageFileNames { get; private set; }

        AppDbContext _artworkContext;

        public bool RecommendationGenerated { get; private set; } = false;

        public FavoritesModel(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public void OnGet()
        {
            var user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            RecommendationGenerated = user.Recommendations.Count != 0;
            ImageFileNames = user.Favorites;
        }


    }
}
