using frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class FavoritesController : Controller
    {
        AppDbContext _artworkContext;

        public FavoritesController(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
