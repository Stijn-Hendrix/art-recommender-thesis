using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace frontend.Pages
{
    public enum SurveyState
    {
        Next,
        Finish
    }

    public class RecommendationsModel : PageModel
    {
        public List<string> ImageFileName { get; private set; } = new List<string>();
        public List<string> ImageFileDescription { get; private set; } = new List<string>();
        public List<string> ImageFileArtstyle { get; private set; } = new List<string>();
        public List<string> ImageFileTheme { get; private set; } = new List<string>();
        public List<int> ArtstyleEqualsInFavorites { get; private set; } = new List<int>();
        public List<int> ThemeEqualsInFavorites { get; private set; } = new List<int>();
        public int FavoritesCount { get; private set; } = 0;
        public int RecommendationIndexOffset { get; private set; } = 0;

        public bool StudyFinished { get; private set; } = false;
        public int RecommendationCount { get; private set; } = 0;
        public List<List<int>> CategoryRankings { get; private set; } = new List<List<int>>();
        public List<List<string>> Categories { get; private set; } = new List<List<string>>();

        public bool DoRanking { get; private set; } = false;

        public SurveyState SurveyState { get; private set; } = SurveyState.Next;

        public List<Dictionary<string, DataBlock>> ImageFileImportances = new List<Dictionary<string, DataBlock>>();



        AppDbContext _artworkContext;

        public RecommendationsModel(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        private void AddRecommendationInUserSession(UserSession usr, ExplanationType type, int recommendationIndex, Dictionary<string, DataBlock> recommendation)
        {
            UserSessionRecommendation rec = new UserSessionRecommendation(recommendationIndex);

            foreach (var key in recommendation.Keys)
            {
                DataBlock dataBlock = recommendation[key];
                if (dataBlock.Importance > 0)
                {
                    rec.AddCategory(key, dataBlock.Importance, JsonConvert.SerializeObject(dataBlock.Factors));
                }
            }

            // Queue spaghetti :)

            Dictionary<double, string> scoreCategoryMap = new Dictionary<double, string>();
            for (int i = 0; i < rec.Categories.Count; i++)
            {
                scoreCategoryMap[rec.CategoriesImportance[i]] = rec.Categories[i];
            }
            var sorted = scoreCategoryMap.OrderByDescending(x => x.Key).ToList();

            Dictionary<string, int> categoryRank = new Dictionary<string, int>();
            for (int i = 0; i < sorted.Count; i++)
            {
                categoryRank[sorted[i].Value] = i;
            }

            int[] ranks = new int[rec.Categories.Count];
            
            for (int i = 0; i < rec.Categories.Count; i++)
            {
                rec.CategoriesRanking.Add(0);
            }
            for (int i = 0; i < rec.Categories.Count; i++)
            {
                rec.CategoriesRanking[i] = categoryRank[rec.Categories[i]];
            }

            rec.ExplanationType = type;
            usr.Recommendations.Add(rec);
            _artworkContext.SaveChanges();
        }

        public static List<List<ExplanationType>> RecommendationConditions = new List<List<ExplanationType>>() {
            new List<ExplanationType>()
            {
                    ExplanationType.Objects, ExplanationType.Objects, ExplanationType.Objects,
                    ExplanationType.Color, ExplanationType.Color, ExplanationType.Color,
                    ExplanationType.Semantics, ExplanationType.Semantics,  ExplanationType.Semantics,
                    ExplanationType.Theme, ExplanationType.Theme,  ExplanationType.Theme,
                    ExplanationType.Artstyle, ExplanationType.Artstyle, ExplanationType.Artstyle,
                    ExplanationType.Description, ExplanationType.Description, ExplanationType.Description,
                    ExplanationType.All, ExplanationType.All, ExplanationType.All
            },
            new List<ExplanationType>()
            {
                    ExplanationType.Theme, ExplanationType.Theme,  ExplanationType.Theme,
                    ExplanationType.Semantics, ExplanationType.Semantics,  ExplanationType.Semantics,
                    ExplanationType.Color, ExplanationType.Color, ExplanationType.Color,
                    ExplanationType.Objects, ExplanationType.Objects, ExplanationType.Objects,
                    ExplanationType.Description, ExplanationType.Description, ExplanationType.Description,
                    ExplanationType.Artstyle, ExplanationType.Artstyle, ExplanationType.Artstyle,
                    ExplanationType.All, ExplanationType.All, ExplanationType.All
            },
            new List<ExplanationType>()
            {
                    ExplanationType.Description, ExplanationType.Description, ExplanationType.Description,
                    ExplanationType.Color, ExplanationType.Color, ExplanationType.Color,
                    ExplanationType.Semantics, ExplanationType.Semantics,  ExplanationType.Semantics,
                    ExplanationType.Theme, ExplanationType.Theme,  ExplanationType.Theme,
                    ExplanationType.Objects, ExplanationType.Objects, ExplanationType.Objects,
                    ExplanationType.Artstyle, ExplanationType.Artstyle, ExplanationType.Artstyle,
                    ExplanationType.All, ExplanationType.All, ExplanationType.All
            },
            new List<ExplanationType>()
            {
                    ExplanationType.Color, ExplanationType.Color, ExplanationType.Color,
                    ExplanationType.Objects, ExplanationType.Objects, ExplanationType.Objects,
                    ExplanationType.Semantics, ExplanationType.Semantics,  ExplanationType.Semantics,
                    ExplanationType.Description, ExplanationType.Description, ExplanationType.Description,
                    ExplanationType.Artstyle, ExplanationType.Artstyle, ExplanationType.Artstyle,
                    ExplanationType.Theme, ExplanationType.Theme,  ExplanationType.Theme,
                    ExplanationType.All, ExplanationType.All, ExplanationType.All
            },
        };

        public static Dictionary<string, ExplanationType> ToExplanationType = new Dictionary<string, ExplanationType>() {
            { "objects",  ExplanationType.Objects },
            { "colors",  ExplanationType.Color },
            { "semantics",  ExplanationType.Semantics },
            { "theme",  ExplanationType.Theme },
            { "artstyle",  ExplanationType.Artstyle },
            { "description",  ExplanationType.Description }
        };


        private static List<ExplanationType> StringCategoriesToExplanationTypes(List<string> categories)
        {
            List<ExplanationType> types = new List<ExplanationType>();
            foreach(var cat in categories)
            {
                for (int i = 0; i < Globals.RecommendationsPerType; i++)
                {
                    types.Add(ToExplanationType[cat]);
                }
            }
            for (int i = 0; i < Globals.RecommendationsPerType; i++)
            {
                types.Add(ExplanationType.All);
            }
            return types;
        }

        private void SaveRecommendations(UserSession user, List<string> categories, List<Dictionary<string, DataBlock>> importances, List<string> recstrings)
        {
            user.Recommendations.Clear();

            List<ExplanationType> types = StringCategoriesToExplanationTypes(categories);
            for (int i = 0; i < Globals.RecommmendationsCount; i++)
            {
                AddRecommendationInUserSession(user, types[i], i, importances[i]);
            }

            user.RecommendationStrings = recstrings;
            _artworkContext.SaveChanges();
        }

        private List<Dictionary<string, DataBlock>> ExtractRecommendationImportances(Recommendation? recommendation)
        {
            return recommendation.Recommendations
                .SelectMany(imageDataDict => imageDataDict
                    .Select(imageData => new Dictionary<string, DataBlock>
                    {
                        {"Objects", imageData.Value.Objects},
                        {"Colors", imageData.Value.Colors},
                        {"Semantics", imageData.Value.Semantics},
                        {"Description", imageData.Value.Description},
                        {"Theme", imageData.Value.Theme},
                        {"Artstyle", imageData.Value.ArtStyle}
                    })
            ).ToList();
        }

        private int CalculateRecommendationIndexOffset(UserSession user)
        {
            var currentExplanationType = user.CurrentExplanationType();
            return user.Recommendations.OrderBy(e => e.RecommendationIndex).ToList().FindIndex(e => e.ExplanationType == currentExplanationType);
        }

        public void OnGet(int recommendationIndex = 0)
        {
            UserSession user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            List<string> favorites = user.Favorites;

            if (favorites.Count < Globals.FavoritesNeeded)
            {
                return;
            }

            StudyFinished = user.StudyFinished(_artworkContext);

			if (user.RecommendationScreenStartTime == DateTime.MinValue)
            {
                user.RecommendationScreenStartTime = DateTime.Now;
            }

            FavoritesCount = favorites.Count;
            if (favorites.Count == 0)
            {
                return;
            }



            List<Artwork> favoriteArtworks = user.GetFavoriteArtworks(_artworkContext);

            
            Recommendation? recommendation = ArtworkController.FindRecommendation(favorites, user.Condition).Result;
            ImageFileName = recommendation.Recommendations.SelectMany(e => e.Keys).ToList();

            ImageFileImportances = ExtractRecommendationImportances(recommendation);
            
            bool recommendationsSaved = user.RecommendationStrings.Count == Globals.RecommmendationsCount;
            if (!recommendationsSaved)
            {
                SaveRecommendations(user, recommendation.Categories, ImageFileImportances, ImageFileName);
            }

            RecommendationCount = user.Recommendations.Count;

            RecommendationIndexOffset = CalculateRecommendationIndexOffset(user);

            var currentExplanationType = user.CurrentExplanationType();
            DoRanking = currentExplanationType == ExplanationType.All;

            SurveyState = DoRanking ? SurveyState.Finish : SurveyState.Next;

            foreach (var rec in user.Recommendations.OrderBy(e => e.RecommendationIndex).ToList())
            {
                CategoryRankings.Add(rec.CategoriesRanking);
                Categories.Add(rec.Categories);
            }


            foreach (string filename in ImageFileName)
            {

                Artwork recommendedArtwork = _artworkContext.Artworks.Find(filename)!;


                // Description
                ImageFileDescription.Add(recommendedArtwork.Description);

                // Artstyle
                if (currentExplanationType == ExplanationType.Artstyle || currentExplanationType == ExplanationType.All)
                {
                    List<string> artstylePieces = recommendedArtwork.Artstyle.Split(" ").ToList();
                    ArtstyleEqualsInFavorites.Add(favoriteArtworks.Where(e => e.Artstyle.Split(" ").Intersect(artstylePieces).Count() > 0).Count());
                }
                else
                {
                    ImageFileArtstyle.Add(recommendedArtwork.Artstyle);
                }
                ImageFileArtstyle.Add(recommendedArtwork.Artstyle);


                // Theme
                if (currentExplanationType == ExplanationType.Theme || currentExplanationType == ExplanationType.All)
                {
                    List<string> themePieces = recommendedArtwork.Theme.Split(" ").ToList();
                    ThemeEqualsInFavorites.Add(favoriteArtworks.Where(e => e.Theme.Split(" ").Intersect(themePieces).Count() > 0).Count());
                    
                }
                else
                {
                    ThemeEqualsInFavorites.Add(0);
                }
                ImageFileTheme.Add(recommendedArtwork.Theme);

            }
        }
    }
}
