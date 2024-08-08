using frontend.Pages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using static frontend.Models.AppDbContext;

namespace frontend.Models
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<Artwork> Artworks { get; set; }

        public DbSet<UserSession> Users { get; set; }

        public AppDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(_configuration.GetConnectionString("Db"));
        }

        internal static string GetStringSha256Hash(string text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        public string Hash(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public bool CreateOrContinueSession(HttpContext context, string userEmail, bool nextIteration = false)
        {
            string hashedEmail = nextIteration ? userEmail : Hash(userEmail);

            Dictionary<string, string> hashes = new Dictionary<string, string>();
            List<string> emails = new List<string>() {
                "anne_hendrix@yahoo.com",
                "anne.hendrix@yahoo.com",
				"joep.demollin@student.kuleuven.be",
				"nathanvanrompuy@gmail.com",
				"jonas.vanhove@student.kuleuven.be",
				"felixdecolvenaer@gmail.com",
				"bert.vanrompuy@hotmail.com",
				"pjotr.lauryssen@student.kuleuven.be",
				"yurryt.vermeire@student.kuleuven.be",
				"mpotargent@yahoo.com",
				"yves_hendrix@yahoo.com",
                "piet.vandamme@outlook.com",
                "Linda.maciel@skynet.be",
                "lisa_vandamme@hotmail.com",
                "johan.vanrompuy@skynet.be",
                "kristin.aerts@skynet.be",
                "Ilsevrompuy@gmail.com"
            };

            foreach(var email in emails)
            {
                hashes.Add(email, Hash(email));
            }

            List<UserSession> sessions = Users.Where(u => u.Email == hashedEmail).ToList();
            List<UserSession> unfinishedSessions = sessions.Where(u => !u.Finished).ToList();

            if (unfinishedSessions.Count > 0)
            {
                ContinueSession(context, unfinishedSessions[0]);
                return true;
            }

            const int taskId = 0; // Use first/only task
            CreateSession(context, hashedEmail, taskId);

            return true;
        }

        private void ContinueSession(HttpContext context, UserSession user)
        {
            context.Session.SetString("email", user.Email);
        }

        private void CreateSession(HttpContext context, string userEmail, int task)
        {
            UserSession session = new UserSession(userEmail, task);

            Users.Add(session);
            SaveChanges();

            context.Session.SetString("email", userEmail);
        }


        public void SeedArtworks()
        {
            string datasetPath = Globals.DatasetPath;

            // Check if the folder exists
            if (File.Exists(datasetPath))
            {
                foreach (Artwork art in Artworks)
                {
                    Artworks.Remove(art);
                }

                string jsonContent = System.IO.File.ReadAllText(datasetPath);

                // Deserialize the JSON content into an Annotations object
                AnnotationsContainer annotationsContainer = JsonConvert.DeserializeObject<AnnotationsContainer>(jsonContent);

                HashSet<string> names = new HashSet<string>();

                int a = 0;
                // Access the data as needed
                foreach (var image in annotationsContainer.Annotations)
                {
                    a++;

                    Artwork artwork = new Artwork();

                    if (names.Contains(image.Image_Path))
                    {
                        string test = image.Image_Path;
                        continue;

                    }
                    names.Add(image.Image_Path);

                    artwork.ImagePath = image.Image_Path;
                    artwork.Artstyle = image.Artstyle;
                    artwork.Theme = image.Theme;
                    artwork.Colors = image.Colors;
                    artwork.Objects = image.Objects;
                    artwork.Semantics = image.Semantics;
                    artwork.Description = image.Description;

                    if (artwork.ImagePath == null)
                    {
                        int b = 1;
                    }

                    Artworks.Add(artwork);
                }

                SaveChanges();

                Console.Write(a);

            }

        }

        // Define a class to represent the structure of the annotations object
        public class AnnotationsContainer
        {
            public List<Annotation> Annotations { get; set; }
        }

        // Define a class to represent the structure of each image
        public class Annotation
        {
			public string Image_Path { get; set; }
			public string Artstyle { get; set; }
			public string Theme { get; set; }
			public List<string> Colors { get; set; }
			public List<string> Objects { get; set; }
			public List<string> Semantics { get; set; }
			public string Description { get; set; }
		}


    }
}
