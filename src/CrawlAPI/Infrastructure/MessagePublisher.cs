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
        if (_connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is not available. Message of type {MessageType} will not be published.", typeof(T).Name);
            return;
        }

        try
        {
            using (var channel = _connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

                var json = JsonConvert.SerializeObject(message);
                var body = System.Text.Encoding.UTF8.GetBytes(json);

                var routingKey = GetRoutingKey<T>();

                var basicProperties = channel.CreateBasicProperties();
                basicProperties.Persistent = true;
                basicProperties.DeliveryMode = 2; // 2 = persistent

                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    basicProperties: basicProperties,
                    body: body
                );

                _logger.LogInformation("Published {MessageType} to {Exchange} with routing key {RoutingKey}",
                    typeof(T).Name, exchangeName, routingKey);
            }

            await Task.CompletedTask;
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
