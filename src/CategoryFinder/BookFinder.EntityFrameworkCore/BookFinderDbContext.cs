using Domain;
using Microsoft.EntityFrameworkCore;

namespace BookFinder.EntityFrameworkCore
{
    public class BookFinderDbContext : DbContext
    {

        public DbSet<PageHtml> PageHtmls { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (optionsBuilder.IsConfigured)
            {
                var connectionString = BookFinder.Tools.Common.GetSettings("ConnectionStrings:Default");
                optionsBuilder.UseNpgsql(connectionString, builder => builder.EnableRetryOnFailure());
            }
        }
    }
}
