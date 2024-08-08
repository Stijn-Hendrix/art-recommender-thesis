using frontend.Controllers;
using frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Web;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace frontend.Pages
{
    public class ArtworkSelectionModel : PageModel
    {
        public List<string> ImageFileNames { get; private set; }
        public int PageSize { get; private set; } = 10; // Set the number of images to display per page
        public int TotalPages { get; private set; }
        public int CurrentPage { get; private set; }
        public string Query { get; private set; }
        public string TaskName { get; private set; }
        public string TaskDescription { get; private set; }
        public int FavoritesCount { get; private set; }
        public List<string> Favorites { get; private set; }

        public bool RecommendationGenerated { get; private set; } = false;


        AppDbContext _artworkContext;

        public ArtworkSelectionModel(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public void OnGet(int selectionpage = 1, string query = "")
        {
            var user = UserController.GetCurrentUser(_artworkContext, HttpContext);
            var taskIndex = user.Task;
            TaskName = Globals.TaskNames[taskIndex];
            TaskDescription = Globals.TaskDescriptions[taskIndex];

            RecommendationGenerated = user.Recommendations.Count != 0;

            Favorites = user.Favorites;
            FavoritesCount = user.Favorites.Count();

            CurrentPage = selectionpage;

            Query = HttpUtility.HtmlEncode(query);

            List<string> keywords =
                query.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .Where(keyword => !_stops.Contains(keyword))
                .ToList();

            if (query != "")
            {
                user.SearchQueriesCount += 1;
                user.SearchQueries.Add(query);
                _artworkContext.SaveChanges();

                var matches = 
                    _artworkContext.Artworks
                        .AsEnumerable()
                        .Where(artwork =>
                            keywords.Any(keyword => artwork.Objects.Select(o => o.ToLower()).Contains(keyword)) ||
                            keywords.Any(keyword => artwork.Colors.Select(o => o.ToLower()).Contains(keyword)) ||
                            keywords.Any(keyword => artwork.Semantics.Select(o => o.ToLower()).Contains(keyword)) ||
                            keywords.Any(keyword => artwork.Description.Split(" ").Select(o => o.ToLower()).Contains(keyword)) ||
                            keywords.Any(keyword => keyword.ToLower() == artwork.Theme.ToLower()) ||
                            keywords.Any(keyword => keyword.ToLower() == artwork.Artstyle.ToLower())
                        ).ToList();


                SetArtworks(selectionpage, matches);
            }
            else
            {
                SetArtworks(selectionpage, _artworkContext.Artworks.ToList());
            }
            
        }

        private void SetArtworks(int selectionpage, List<Artwork> artworks)
        {
            TotalPages = artworks.Count() / PageSize + 1;

            selectionpage = Math.Max(0, selectionpage);
            selectionpage = Math.Min(selectionpage, TotalPages);

            CurrentPage = selectionpage;
            ImageFileNames = artworks.Take(new Range((selectionpage - 1) * PageSize, selectionpage * PageSize)).Select(e => e.ImagePath).ToList();
        }

        static HashSet<string> _stops = new HashSet<string>()
        {
        { "a" },
        { "about" },
        { "above" },
        { "across" },
        { "after" },
        { "afterwards" },
        { "again" },
        { "against" },
        { "all" },
        { "almost" },
        { "alone" },
        { "along" },
        { "already" },
        { "also" },
        { "although" },
        { "always" },
        { "am" },
        { "among" },
        { "amongst" },
        { "amount" },
        { "an" },
        { "and" },
        { "another" },
        { "any" },
        { "anyhow" },
        { "anyone" },
        { "anything" },
        { "anyway" },
        { "anywhere" },
        { "are" },
        { "around" },
        { "as" },
        { "at" },
        { "back" },
        { "be" },
        { "became" },
        { "because" },
        { "become" },
        { "becomes" },
        { "becoming" },
        { "been" },
        { "before" },
        { "beforehand" },
        { "behind" },
        { "being" },
        { "below" },
        { "beside" },
        { "besides" },
        { "between" },
        { "beyond" },
        { "bill" },
        { "both" },
        { "bottom" },
        { "but" },
        { "by" },
        { "call" },
        { "can" },
        { "cannot" },
        { "cant" },
        { "co" },
        { "computer" },
        { "con" },
        { "could" },
        { "couldnt" },
        { "cry" },
        { "de" },
        { "describe" },
        { "detail" },
        { "do" },
        { "done" },
        { "down" },
        { "due" },
        { "during" },
        { "each" },
        { "eg" },
        { "eight" },
        { "either" },
        { "eleven" },
        { "else" },
        { "elsewhere" },
        { "empty" },
        { "enough" },
        { "etc" },
        { "even" },
        { "ever" },
        { "every" },
        { "everyone" },
        { "everything" },
        { "everywhere" },
        { "except" },
        { "few" },
        { "fifteen" },
        { "fify" },
        { "fill" },
        { "find" },
        { "fire" },
        { "first" },
        { "five" },
        { "for" },
        { "former" },
        { "formerly" },
        { "forty" },
        { "found" },
        { "four" },
        { "from" },
        { "front" },
        { "full" },
        { "further" },
        { "get" },
        { "give" },
        { "go" },
        { "had" },
        { "has" },
        { "have" },
        { "he" },
        { "hence" },
        { "her" },
        { "here" },
        { "hereafter" },
        { "hereby" },
        { "herein" },
        { "hereupon" },
        { "hers" },
        { "herself" },
        { "him" },
        { "himself" },
        { "his" },
        { "how" },
        { "however" },
        { "hundred" },
        { "i" },
        { "ie" },
        { "if" },
        { "in" },
        { "inc" },
        { "indeed" },
        { "interest" },
        { "into" },
        { "is" },
        { "it" },
        { "its" },
        { "itself" },
        { "keep" },
        { "last" },
        { "latter" },
        { "latterly" },
        { "least" },
        { "less" },
        { "ltd" },
        { "made" },
        { "many" },
        { "may" },
        { "me" },
        { "meanwhile" },
        { "might" },
        { "mill" },
        { "mine" },
        { "more" },
        { "moreover" },
        { "most" },
        { "mostly" },
        { "move" },
        { "much" },
        { "must" },
        { "my" },
        { "myself" },
        { "name" },
        { "namely" },
        { "neither" },
        { "never" },
        { "nevertheless" },
        { "next" },
        { "nine" },
        { "no" },
        { "nobody" },
        { "none" },
        { "nor" },
        { "not" },
        { "nothing" },
        { "now" },
        { "nowhere" },
        { "of" },
        { "off" },
        { "often" },
        { "on" },
        { "once" },
        { "one" },
        { "only" },
        { "onto" },
        { "or" },
        { "other" },
        { "others" },
        { "otherwise" },
        { "our" },
        { "ours" },
        { "ourselves" },
        { "out" },
        { "over" },
        { "own" },
        { "part" },
        { "per" },
        { "perhaps" },
        { "please" },
        { "put" },
        { "rather" },
        { "re" },
        { "same" },
        { "see" },
        { "seem" },
        { "seemed" },
        { "seeming" },
        { "seems" },
        { "serious" },
        { "several" },
        { "she" },
        { "should" },
        { "show" },
        { "side" },
        { "since" },
        { "sincere" },
        { "six" },
        { "sixty" },
        { "so" },
        { "some" },
        { "somehow" },
        { "someone" },
        { "something" },
        { "sometime" },
        { "sometimes" },
        { "somewhere" },
        { "still" },
        { "such" },
        { "system" },
        { "take" },
        { "ten" },
        { "than" },
        { "that" },
        { "the" },
        { "their" },
        { "them" },
        { "themselves" },
        { "then" },
        { "thence" },
        { "there" },
        { "thereafter" },
        { "thereby" },
        { "therefore" },
        { "therein" },
        { "thereupon" },
        { "these" },
        { "they" },
        { "thick" },
        { "thin" },
        { "third" },
        { "this" },
        { "those" },
        { "though" },
        { "three" },
        { "through" },
        { "throughout" },
        { "thru" },
        { "thus" },
        { "to" },
        { "together" },
        { "too" },
        { "top" },
        { "toward" },
        { "towards" },
        { "twelve" },
        { "twenty" },
        { "two" },
        { "un" },
        { "under" },
        { "until" },
        { "up" },
        { "upon" },
        { "us" },
        { "very" },
        { "via" },
        { "was" },
        { "we" },
        { "well" },
        { "were" },
        { "what" },
        { "whatever" },
        { "when" },
        { "whence" },
        { "whenever" },
        { "where" },
        { "whereafter" },
        { "whereas" },
        { "whereby" },
        { "wherein" },
        { "whereupon" },
        { "wherever" },
        { "whether" },
        { "which" },
        { "while" },
        { "whither" },
        { "who" },
        { "whoever" },
        { "whole" },
        { "whom" },
        { "whose" },
        { "why" },
        { "will" },
        { "with" },
        { "within" },
        { "without" },
        { "would" },
        { "yet" },
        { "you" },
        { "your" },
        { "yours" },
        { "yourself" },
        { "yourselves" }
        };

    }
}
