using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FcgCatalogApi.Events;

public sealed class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchangeName;
    private bool _isConnected;

    public RabbitMQEventPublisher(IConfiguration configuration, ILogger<RabbitMQEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "fcg.catalog.events";
        TryConnect();
    }

    private void TryConnect()
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

            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _isConnected = true;
            _logger.LogInformation("Conectado ao RabbitMQ - Exchange: {Exchange}", _exchangeName);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ. Eventos não serão publicados.");
        }
    }

    public Task PublishAsync<T>(T @event) where T : class
    {
        if (!_isConnected || _channel is null)
        {
            _logger.LogWarning("RabbitMQ não conectado. Evento {EventType} não publicado.", typeof(T).Name);
            return Task.CompletedTask;
        }

        try
        {
            var eventName = typeof(T).Name;
            var routingKey = $"catalog.{eventName.ToLowerInvariant()}";
            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2; // persistent
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = eventName;

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Evento publicado: {EventType} - RoutingKey: {RoutingKey}", eventName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento {EventType}", typeof(T).Name);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
