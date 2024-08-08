using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace frontend.Controllers
{
    [Route("api/artworks")]
    [ApiController]
    public class ArtworkController : ControllerBase
    {

        AppDbContext _artworkContext;

        public ArtworkController(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }


		[HttpGet("/Test")]
		public IActionResult Test()
		{
			// Process the responses here, you can save them to a database or perform any other actions

			return RedirectToAction("ThankYouPage"); // Redirect to a thank you page or any other appropriate action
		}

		[HttpGet("GetImageDescription/{imageName}")]
        public string[] GetImageDescription(string imageName)
        {
            Artwork? artwork = _artworkContext.Artworks.Find(imageName);
            return artwork != null? new string[] { artwork.Theme, artwork.Artstyle, artwork.Description } :  new string[] { "No info found!" };
        }

        [HttpPost("AddFavorite/{imageName}")]
        public void AddFavorite(string imageName)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);

            if (!user.Favorites.Contains(imageName))
            {
                user.Favorites.Add(imageName);
                _artworkContext.SaveChanges();
            }
            else
            {
                user.Favorites.Remove(imageName);
                _artworkContext.SaveChanges();
            }
        }

        [HttpGet("FavoritesCount/")]
        public int FavoritesCount()
        {
            return UserController.GetCurrentUser(_artworkContext, HttpContext).Favorites.Count();
        }

        [HttpPost("NextIteration/")]
        public void NextIteration()
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.Finished = true;
            user.EndTime = DateTime.Now;
            _artworkContext.SaveChanges();

            _artworkContext.CreateOrContinueSession(HttpContext, user.Email, true);

            _artworkContext.SaveChanges();
        }

        // Using ASP.NET Core MVC Routing
        [HttpPost("SetRating/{recommendationIndex}")]
        public void SetRating(int recommendationIndex, [FromQuery] int rating)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.RecommendationRating = rating;
            _artworkContext.SaveChanges();
        }

        [HttpPost("IncreaseRanking/{recommendationIndex}")]
        public void IncreaseRanking(int recommendationIndex, [FromQuery] string category)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.IncreaseCategoryRanking(category);
            _artworkContext.SaveChanges();
        }

        [HttpPost("DecreaseRanking/{recommendationIndex}")]
        public void DecreaseRanking(int recommendationIndex, [FromQuery] string category)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.DecreaseCategoryRanking(category);
            _artworkContext.SaveChanges();
        }

        [HttpPost("SetRelevant/{recommendationIndex}")]
        public void SetRelevant(int recommendationIndex, [FromQuery] string category)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.SetCategoryRating(category, CategoryRating.RELEVANT);
            _artworkContext.SaveChanges();
        }

        [HttpPost("SetNonRelevant/{recommendationIndex}")]
        public void SetNonRelevant(int recommendationIndex, [FromQuery] string category)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.SetCategoryRating(category, CategoryRating.NON_RELEVANT);
            _artworkContext.SaveChanges();
        }

        [HttpGet("GetRelevancy/{recommendationIndex}")]
        public int GetRelevancy(int recommendationIndex, [FromQuery] string category)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            return (int)user.GetRecommendation(recommendationIndex)!.GetCategoryRating(category);
        }

        [HttpGet("GetRelevancies/{recommendationIndex}")]
        public Dictionary<string, CategoryRating> GetRelevancies(int recommendationIndex)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            return user.GetRecommendation(recommendationIndex)!.GetCategoryRatings();
        }


        [HttpGet("GetRating/{recommendationIndex}")]
        public int GetRating(int recommendationIndex)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            return user.GetRecommendation(recommendationIndex)!.RecommendationRating;
        }

        [HttpDelete("DeleteFavorite/{imageName}")]
        public void DeleteFavorite(string imageName)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);

            if (user.Favorites.Contains(imageName))
            {
                user.Favorites.Remove(imageName);
                _artworkContext.SaveChanges();
            }
        }



        [HttpPost("AddHoverData/")]
        public void AddHoverData([FromQuery] int secondsHovered, [FromQuery] string field)
        {
            return;
           
        }

        [HttpPost("AddLookatRecommendation/{recommendationIndex}")]
        public void AddLookatRecommendation(int recommendationIndex, [FromQuery] int seconds)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            user.GetRecommendation(recommendationIndex)!.LookatTimeInSeconds += seconds;
            _artworkContext.SaveChanges();
        }

        public class RecommendationRequest
        {
            [JsonProperty("image_names")]
            public List<string> ImageNames { get; set; }

            [JsonProperty("condition")]
            public int Condition { get; set; }

            public RecommendationRequest(List<string> images, int condition)
            {
                ImageNames = images;
                Condition = condition;
            }
        }


        public static async Task<Recommendation> FindRecommendation(List<string> images, int condition)
        {
            if (images.Count == 0) return new Recommendation();


            using (HttpClient client = new HttpClient())
            {
                try
                {
                    RecommendationRequest req = new RecommendationRequest(images, condition);
                    string jsonImages = JsonConvert.SerializeObject(req);

                    HttpContent content = new StringContent(jsonImages, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:8000/recommendation", content);


                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        // Deserialize the JSON response into a C# object
                        var t = JsonConvert.DeserializeObject<Recommendation>(jsonResponse);

                        var responseObject = JsonConvert.DeserializeObject<Recommendation>(jsonResponse);


                        return responseObject;
                        //recommendations.From(responseObject);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            return new Recommendation();
        }

       
    }
}

public class Recommendation
{
	public List<Dictionary<string, ImageDetails>> Recommendations { get; set; }
    public List<string> Categories { get; set; }
}

public class ImageDetails
{
	public DataBlock Objects { get; set; }
	public DataBlock Colors { get; set; }
	public DataBlock Semantics { get; set; }
	public DataBlock Description { get; set; }
	public DataBlock Theme { get; set; }
	public DataBlock ArtStyle { get; set; }
}

public class DataBlock
{
	public double Importance { get; set; }
	public Dictionary<string, double> Factors { get; set; }
}