namespace frontend.Data
{
    using frontend.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public class DbSeeder
    {
        private AppDbContext _artworkContext;

        public DbSeeder(AppDbContext artworkContext)
        {
            _artworkContext = artworkContext;
        }

        public void Seed()
        {
            _artworkContext.SeedArtworks();
        }

    }
}
