using FcgCatalogApi.Models;

namespace FcgCatalogApi.DTOs;

/// <summary>Item da biblioteca do usuário.</summary>
public sealed record BibliotecaItemResponse(
    Guid Id,
    Guid GameId,
    string? GameTitle,
    Guid OrderId,
    decimal PrecoPago,
    DateTime AdquiridoEm);
