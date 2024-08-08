using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace frontend.Controllers
{
    public class UserController : Controller
    {
        static AppDbContext _artworkContext;

        public UserController(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public IActionResult Start([FromForm] string email)
        {
            if (_artworkContext.CreateOrContinueSession(HttpContext, email, false))
            {
                return Redirect("/ArtworkSelection");

            }
            return Redirect("/");
        }

        public static void FinishCurrentUser(AppDbContext db, HttpContext context)
        {
            var user = GetCurrentUser(db, context);
            user.Finished = true;
            user.EndTime = DateTime.Now;
            db.SaveChanges();
            context.Session.Remove("email");
        }

        public static UserSession GetCurrentUser(AppDbContext db, HttpContext context)
        {
            string? email = context.Session.GetString("email");
            try
            {
                var a = db.Users.Include(u => u.Recommendations);
                var c = a.Where(u => u.Email == email);
                var d = c.First(u => !u.Finished);
                return d;
            }
            catch (Exception e)
            {
                /*
                var a = db.Users.Include(u => u.Recommendations);
                var b = a.Include(u => u.HoverDatas);
                var c = b.Where(u => u.Email == email);
                var d = c.First(u => !u.Finished);
                */
                throw new Exception("User session expired! Try login in again.");
            }
        }

        public static List<UserSession> GetAllUserSessions(AppDbContext db, string userId)
        {
            return db.Users.Include(u => u.Recommendations).Where(u => u.Email == userId).ToList();
        }
    }
}
