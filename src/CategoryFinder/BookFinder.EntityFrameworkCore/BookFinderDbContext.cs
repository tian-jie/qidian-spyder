using Domain;
using Microsoft.EntityFrameworkCore;
using System;

namespace BookFinder.EntityFrameworkCore
{
    public class BookFinderDbContext : DbContext
    {

        public DbSet<PageHtml> PageHtmls { get; set; }
        public DbSet<StockData> Stocks { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (optionsBuilder.IsConfigured)
            {
                var connectionString = BookFinder.Tools.Common.GetSettings("ConnectionStrings:Default");
                //optionsBuilder.UseNpgsql(connectionString, builder => builder.EnableRetryOnFailure());
                //optionsBuilder.UseMySql(connectionString: connectionString, serverVersion: new MySqlServerVersion(new Version(5, 7)));
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}
