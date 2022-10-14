using Microsoft.EntityFrameworkCore;
using NetMetaprograming.Data.Models;

namespace NetMetaprograming.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseInMemoryDatabase("Store");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().HasData
            (
                new()
                {
                    Id = Guid.NewGuid(),
                    Nome = "Item1",
                    Description = "Item1 description"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Nome = "Item2",
                    Description = "Item2 description"
                }
            );
        }
    }
}
