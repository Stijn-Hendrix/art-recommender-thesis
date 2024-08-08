using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace frontend.Models
{

    public enum CategoryRating
    {
        UNSET = 0,
        NON_RELEVANT = 1,
        RELEVANT = 2,
    }

    public enum ExplanationType
    {
        Objects,
        Color,
        Semantics,
        Theme,
        Artstyle,
        Description,
        All
    }

    public class UserSessionRecommendation
    {
        [Key]
        public int ID { get; set; }
        public int RecommendationIndex { get; set; }

        /*
            CATEGORIES RECOMMENDATION
        */
        public List<string> Categories { get; set; }
        public List<double> CategoriesImportance { get; set; }
        public List<string> CategoriesExplanation { get; set; }
        public List<int> CategoriesRanking { get; set; }

        /*
            EXPLANATION
        */
        public ExplanationType ExplanationType { get; set; }

        /*
            RATINGS
        */
        public List<int> CategoriesRelevantcyRatings { get; set; } // 0 = unset, 1 = not relevant, 2 = relevant
        public int RecommendationRating { get; set; }
        public string QuestionnaireResponses { get; set; }

        /*
            DATA
        */
        public int LookatTimeInSeconds { get; set; }

        public UserSessionRecommendation(int recommendationIndex)
        {
            Categories = new List<string>();
            CategoriesImportance = new List<double>();
            CategoriesRelevantcyRatings = new List<int>(); // 0 = non-relevant, 1 = relevant
            RecommendationRating = (int)CategoryRating.UNSET;
            RecommendationIndex = recommendationIndex;
            CategoriesExplanation = new List<string>();
            CategoriesRanking = new List<int>();
            LookatTimeInSeconds = 0;
            QuestionnaireResponses = "";
        }

        public void AddCategory(string name, double importance, string explanation)
        {
            Categories.Add(name);
            CategoriesImportance.Add(importance);
            CategoriesRelevantcyRatings.Add(0);
            CategoriesExplanation.Add(explanation);
        }

        public void IncreaseCategoryRanking(string category)
        {
            int index = Categories.FindIndex(e => e.Equals(category));
            var current = CategoriesRanking[index];

            if (current == 0)
            {
                return;
            }

            CategoriesRanking[CategoriesRanking.FindIndex(e => e == current - 1)] += 1;
            CategoriesRanking[index] -= 1;
        }

        public void DecreaseCategoryRanking(string category)
        {
            int index = Categories.FindIndex(e => e.Equals(category));
            var current = CategoriesRanking[index];

            if (current == CategoriesRanking.Count - 1)
            {
                return;
            }

            CategoriesRanking[CategoriesRanking.FindIndex(e => e == current + 1)] -= 1;
            CategoriesRanking[index] += 1;
        }

        public void SetCategoryRating(string name, CategoryRating rating)
        {
            int index = Categories.FindIndex(e => e.Equals(name));
            CategoriesRelevantcyRatings[index] = (int)rating;
        }

        public CategoryRating GetCategoryRating(string name)
        {
            int index = Categories.FindIndex(e => e.Equals(name));
            return (CategoryRating)CategoriesRelevantcyRatings[index];
        }

        public Dictionary<string, CategoryRating> GetCategoryRatings()
        {
            Dictionary<string, CategoryRating> ratings = new Dictionary<string, CategoryRating>();

            for (int i = 0; i < Categories.Count; i++)
            {
                if (CategoriesImportance[i] > 0)
                {
                    ratings.Add(Categories[i], (CategoryRating)CategoriesRelevantcyRatings[i]);
                }
            }
            return ratings;
        }
    }

    public class UserSession
    {

        /*
            IDENTIFIERS
        */
        [Key]
        public int ID { get; set; }
        public string Email { get; set; }

        // 720 = condition count
        public int Condition { get => ID % 720; }

        /*
            FAVORITES
        */
        public List<string> Favorites { get; set; }

        /*
            RECOMMENDATIONS
        */
        public List<string> RecommendationStrings { get; set; }
        public List<UserSessionRecommendation> Recommendations { get; set; }

        public bool StudyFinished(AppDbContext artworkContext)
        {
            return false;
        }

        private bool RecommendationsFinished()
        {
            if (Recommendations.Count != Globals.RecommmendationsCount)
            {
                return false;
            }

            foreach (var rec in Recommendations)
            {
                foreach (var i in rec.CategoriesRelevantcyRatings)
                {
                    if (i == (int)CategoryRating.UNSET)
                    {
                        return false;
                    }
                }

                if (rec.RecommendationRating == (int)CategoryRating.UNSET)
                {
                    return false;
                }
            }

            return true;
        }

        public int Task { get; set; }

        /* 
            QUESTIONNAIRES
        */

        public bool Finished { get; set; } =  false;
        public string QuestionnaireResponses { get; set; }

        /*
            DATA
        */

        private DateTime _start;
        public DateTime StartTime
        {
            get { return _start; }
            set { _start = value.ToUniversalTime(); }
        }

        private DateTime _end;
        public DateTime EndTime
        {
            get { return _end; }
            set { _end = value.ToUniversalTime(); }
        }

        private DateTime _recommendationStart;
        public DateTime RecommendationScreenStartTime
        {
            get { return _recommendationStart; }
            set { _recommendationStart = value.ToUniversalTime(); }
        }

        public int SearchQueriesCount { get; set; }
        public List<string> SearchQueries { get; set; }

        public List<Artwork> GetFavoriteArtworks(AppDbContext db)
        {
            return Favorites.Select(e => db.Artworks.Find(e)!).ToList();
        }

        public UserSession(string email, int task)
        {
            Email = email;
            Favorites = new List<string>();
            RecommendationStrings = new List<string>();
            Recommendations = new List<UserSessionRecommendation>();
            Finished = false;
            QuestionnaireResponses = "";
            Task = task;

            StartTime = DateTime.Now;

            SearchQueriesCount = 0;
            SearchQueries = new List<string>();
        }

        public UserSessionRecommendation GetRecommendation(int id)
        {
            if (Recommendations.Count != Globals.RecommmendationsCount)
            {
                throw new Exception("You do not have any recommendations yet!");
            }
            return Recommendations.Find(r => r.RecommendationIndex == id)!;
        }

        public ExplanationType CurrentExplanationType()
        {
            var ordered = Recommendations.OrderBy(e => e.RecommendationIndex);
            var explanationType = ordered.First(e => e.QuestionnaireResponses == "").ExplanationType;
            return explanationType;
        }
    }
}
