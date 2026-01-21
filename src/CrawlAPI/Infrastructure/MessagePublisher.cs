using RabbitMQ.Client;
using Newtonsoft.Json;
using SharedDomain.Messages;

namespace CrawlAPI.Infrastructure;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string exchangeName) where T : class;
}

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string exchangeName) where T : class
    {
        try
        {
            using var channel = _connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

            var json = JsonConvert.SerializeObject(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            var routingKey = GetRoutingKey<T>();

            channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );

            _logger.LogInformation("Published {MessageType} to {Exchange} with routing key {RoutingKey}",
                typeof(T).Name, exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }

    private static string GetRoutingKey<T>() where T : class
    {
        return typeof(T).Name.ToLower();
    }
}
