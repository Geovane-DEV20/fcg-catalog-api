using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Game> Games { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<LibraryItem> LibraryItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Library>()
            .HasMany(l => l.Itens)
            .WithOne()
            .HasForeignKey(i => i.LibraryId);
    }
}
