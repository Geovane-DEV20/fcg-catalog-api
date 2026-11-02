using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using FcgCatalogApi.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FcgCatalogApi.Events;

/// <summary>
/// Consumidor de eventos de pagamento do RabbitMQ.
/// - PaymentApproved: Adiciona jogo à biblioteca
/// - PaymentRejected: Apenas loga (não adiciona)
/// - Idempotente: Não duplica se evento for reprocessado
/// </summary>
public sealed class PaymentEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchangeName;
    private readonly string _queueName;

    public PaymentEventConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PaymentEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _exchangeName = _configuration["RabbitMQ:PaymentExchangeName"] ?? "fcg.payments.events";
        _queueName = "fcg.catalog.payment-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Libera thread principal

        try
        {
            SetupRabbitMQ();

            if (_channel is null)
            {
                _logger.LogWarning("RabbitMQ não disponível. Consumer não iniciado.");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    await ProcessMessageAsync(ea, stoppingToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    // Requeue para retry
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("PaymentEventConsumer iniciado. Aguardando eventos de pagamento...");

            // Mantém o serviço rodando
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal no PaymentEventConsumer");
        }
    }

    private void SetupRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange de pagamentos (se não existir)
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declarar fila para este consumer
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind para eventos de pagamento aprovado e rejeitado
            _channel.QueueBind(_queueName, _exchangeName, "payment.approved");
            _channel.QueueBind(_queueName, _exchangeName, "payment.rejected");

            // Prefetch de 1 para processamento ordenado
            _channel.BasicQos(0, 1, false);

            _logger.LogInformation("RabbitMQ configurado. Exchange: {Exchange}, Queue: {Queue}", _exchangeName, _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ para consumo de eventos.");
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var routingKey = ea.RoutingKey;

        _logger.LogInformation("Mensagem recebida. RoutingKey: {RoutingKey}", routingKey);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bibliotecaRepository = scope.ServiceProvider.GetRequiredService<IBibliotecaRepository>();

        if (routingKey == "payment.approved")
        {
            await ProcessPaymentApprovedAsync(message, context, bibliotecaRepository, cancellationToken);
        }
        else if (routingKey == "payment.rejected")
        {
            ProcessPaymentRejected(message);
        }
    }

    private async Task ProcessPaymentApprovedAsync(
        string message,
        ApplicationDbContext context,
        IBibliotecaRepository bibliotecaRepository,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var paymentEvent = JsonSerializer.Deserialize<PaymentApprovedEvent>(message, options);

        if (paymentEvent is null)
        {
            _logger.LogWarning("PaymentApprovedEvent inválido: {Message}", message);
            return;
        }

        _logger.LogInformation(
            "Processando PaymentApproved - OrderId: {OrderId}, UserId: {UserId}, GameId: {GameId}",
            paymentEvent.OrderId, paymentEvent.UserId, paymentEvent.GameId);

        // IDEMPOTÊNCIA: Verificar se já processou este OrderId
        if (await bibliotecaRepository.ExistePorOrderIdAsync(paymentEvent.OrderId, cancellationToken))
        {
            _logger.LogInformation(
                "OrderId {OrderId} já processado anteriormente. Ignorando duplicata.",
                paymentEvent.OrderId);
            return;
        }

        // IDEMPOTÊNCIA: Verificar se usuário já possui o jogo
        if (await bibliotecaRepository.ExisteNaBibliotecaAsync(paymentEvent.UserId, paymentEvent.GameId, cancellationToken))
        {
            _logger.LogInformation(
                "Usuário {UserId} já possui jogo {GameId} na biblioteca. Ignorando.",
                paymentEvent.UserId, paymentEvent.GameId);
            return;
        }

        // Adicionar jogo à biblioteca
        var bibliotecaItem = BibliotecaItem.Criar(
            paymentEvent.UserId,
            paymentEvent.GameId,
            paymentEvent.OrderId,
            paymentEvent.Price);

        await bibliotecaRepository.AdicionarAsync(bibliotecaItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Jogo adicionado à biblioteca - UserId: {UserId}, GameId: {GameId}, OrderId: {OrderId}",
            paymentEvent.UserId, paymentEvent.GameId, paymentEvent.OrderId);
    }

    private void ProcessPaymentRejected(string message)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var paymentEvent = JsonSerializer.Deserialize<PaymentRejectedEvent>(message, options);

        if (paymentEvent is null)
        {
            _logger.LogWarning("PaymentRejectedEvent inválido: {Message}", message);
            return;
        }

        // Pagamento rejeitado: apenas loga, não adiciona à biblioteca
        _logger.LogInformation(
            "Pagamento rejeitado - OrderId: {OrderId}, UserId: {UserId}, GameId: {GameId}, Reason: {Reason}",
            paymentEvent.OrderId, paymentEvent.UserId, paymentEvent.GameId, paymentEvent.Reason);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
