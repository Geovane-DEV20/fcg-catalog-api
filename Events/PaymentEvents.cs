namespace FcgCatalogApi.Events;

/// <summary>
/// Evento recebido quando um pagamento é aprovado.
/// Dispara a adição do jogo à biblioteca do usuário.
/// </summary>
public sealed record PaymentApprovedEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    DateTime ApprovedAt);

/// <summary>
/// Evento recebido quando um pagamento é rejeitado.
/// Não adiciona o jogo à biblioteca.
/// </summary>
public sealed record PaymentRejectedEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    string Reason,
    DateTime RejectedAt);
