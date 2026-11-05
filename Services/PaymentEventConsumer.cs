using System.Text.Json;
using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Services;

public class PaymentEventConsumer : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentEventConsumer> _logger;

    public PaymentEventConsumer(
        IRabbitMqService rabbitMqService,
        IServiceProvider serviceProvider,
        ILogger<PaymentEventConsumer> logger)
    {
        _rabbitMqService = rabbitMqService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _rabbitMqService.StartConsuming("payment.processed", async (message) =>
        {
            try
            {
                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
                if (paymentEvent != null && paymentEvent.Status == "Approved")
                {
                    await AddGameToLibrary(paymentEvent.UserId, paymentEvent.GameId, paymentEvent.Price);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar evento de pagamento: {ex.Message}");
            }
        });

        return Task.CompletedTask;
    }

    private async Task AddGameToLibrary(Guid userId, Guid gameId, decimal price)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var library = await dbContext.Libraries
            .Include(l => l.Itens)
            .FirstOrDefaultAsync(l => l.UsuarioId == userId);

        if (library == null)
        {
            library = new Library
            {
                Id = Guid.NewGuid(),
                UsuarioId = userId
            };
            dbContext.Libraries.Add(library);
        }

        // Verificar se o jogo já não está na biblioteca
        if (!library.Itens.Any(i => i.JogoId == gameId))
        {
            library.Itens.Add(new LibraryItem
            {
                Id = Guid.NewGuid(),
                LibraryId = library.Id,
                JogoId = gameId,
                ValorPago = price,
                AdquiridoEm = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"Jogo {gameId} adicionado à biblioteca do usuário {userId}");
        }
    }
}
