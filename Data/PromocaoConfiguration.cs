using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FcgCatalogApi.Data;

public sealed class PromocaoConfiguration : IEntityTypeConfiguration<Promocao>
{
    public void Configure(EntityTypeBuilder<Promocao> builder)
    {
        builder.ToTable("Promocoes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).HasMaxLength(200).IsRequired();
        builder.Property(p => p.PercentualDesconto).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(p => p.VigenciaInicio).IsRequired();
        builder.Property(p => p.VigenciaFim).IsRequired();
        builder.Property(p => p.EncerradaManualmente).IsRequired();

        // Armazenar JogoIds como JSON
        builder.Property(p => p.JogoIdsInterno)
            .HasColumnName("JogoIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            );

        builder.Ignore(p => p.JogoIds);
    }
}
