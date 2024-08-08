using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages
{
    public class FavoritesViewOnlyModel : PageModel
    {
        public List<string> ImageFileNames { get; private set; }

        AppDbContext _artworkContext;

        public FavoritesViewOnlyModel(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public void OnGet()
        {
            ImageFileNames = UserController.GetCurrentUser(_artworkContext, HttpContext).Favorites;
        }

    }
}
