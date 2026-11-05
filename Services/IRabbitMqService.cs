namespace FcgCatalogApi.Services;

public interface IRabbitMqService
{
    void PublishMessage(string routingKey, object message);
    void StartConsuming(string routingKey, Action<string> messageHandler);
}
