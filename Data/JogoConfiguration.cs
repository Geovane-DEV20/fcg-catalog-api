using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FcgCatalogApi.Data;

public sealed class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.ToTable("Jogos");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Descricao).HasMaxLength(4000);
        builder.Property(j => j.Genero).HasMaxLength(100);
        builder.Property(j => j.Desenvolvedora).HasMaxLength(200);
        builder.Property(j => j.Preco).HasColumnType("decimal(18,2)");
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(j => j.CriadoEm).IsRequired();

        builder.HasIndex(j => j.Titulo).IsUnique();
    }
}
