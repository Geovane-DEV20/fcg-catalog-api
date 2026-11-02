using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Data;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<BibliotecaItem> BibliotecaItens => Set<BibliotecaItem>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);
}
