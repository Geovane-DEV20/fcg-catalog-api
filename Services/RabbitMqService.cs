using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FcgCatalogApi.Services;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqService> _logger;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "fcg-events", type: ExchangeType.Topic, durable: true);
            
            _logger.LogInformation("Conectado ao RabbitMQ com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Não foi possível conectar ao RabbitMQ: {ex.Message}");
        }
    }

    public void PublishMessage(string routingKey, object message)
    {
        if (_channel == null)
        {
            _logger.LogWarning("Canal RabbitMQ não disponível. Mensagem não publicada.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "fcg-events",
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );

            _logger.LogInformation($"Mensagem publicada: {routingKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao publicar mensagem: {ex.Message}");
        }
    }

    public void StartConsuming(string routingKey, Action<string> messageHandler)
    {
        if (_channel == null)
        {
            _logger.LogWarning("Canal RabbitMQ não disponível. Não é possível consumir mensagens.");
            return;
        }

        try
        {
            var queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: queueName, exchange: "fcg-events", routingKey: routingKey);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation($"Mensagem recebida [{routingKey}]: {message}");
                messageHandler(message);
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
            _logger.LogInformation($"Consumindo mensagens da fila: {routingKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao consumir mensagens: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
