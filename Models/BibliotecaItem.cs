namespace FcgCatalogApi.Models;

/// <summary>
/// Item da biblioteca do usuário - representa um jogo adquirido.
/// </summary>
public sealed class BibliotecaItem
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal PrecoPago { get; private set; }
    public DateTime AdquiridoEm { get; private set; }

    private BibliotecaItem() { }

    public static BibliotecaItem Criar(Guid userId, Guid gameId, Guid orderId, decimal precoPago)
    {
        return new BibliotecaItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            OrderId = orderId,
            PrecoPago = precoPago,
            AdquiridoEm = DateTime.UtcNow
        };
    }
}
