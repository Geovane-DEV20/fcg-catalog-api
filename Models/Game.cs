namespace FcgCatalogApi.Models;

public class Game
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string? Desenvolvedora { get; set; }
    public DateTime? DataLancamento { get; set; }
    public decimal Preco { get; set; }
    public string Status { get; set; } = "Publicado";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Library
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public List<LibraryItem> Itens { get; set; } = new();
}

public class LibraryItem
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public Guid JogoId { get; set; }
    public decimal ValorPago { get; set; }
    public DateTime AdquiridoEm { get; set; }
}

public class OrderPlacedEvent
{
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public Guid OrderId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PaymentProcessedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty; // "Approved" or "Rejected"
    public DateTime Timestamp { get; set; }
}
