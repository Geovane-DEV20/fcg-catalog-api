using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FcgCatalogApi.Data;

public sealed class BibliotecaItemConfiguration : IEntityTypeConfiguration<BibliotecaItem>
{
    public void Configure(EntityTypeBuilder<BibliotecaItem> builder)
    {
        builder.ToTable("BibliotecaItens");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserId).IsRequired();
        builder.Property(b => b.GameId).IsRequired();
        builder.Property(b => b.OrderId).IsRequired();
        builder.Property(b => b.PrecoPago).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.AdquiridoEm).IsRequired();

        // Índice único para evitar duplicidade: mesmo usuário + mesmo jogo
        builder.HasIndex(b => new { b.UserId, b.GameId }).IsUnique();

        // Índice único por OrderId para idempotência
        builder.HasIndex(b => b.OrderId).IsUnique();
    }
}
