using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using SharedDomain.Messages;

namespace CrawlWorker.Infrastructure;

public interface IMessageConsumer
{
    void SubscribeToJobCreated(Func<CrawlJobCreated, Task> handler);
    void Start();
}

public class RabbitMqConsumer : IMessageConsumer
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IModel? _channel;
    private Func<CrawlJobCreated, Task>? _jobCreatedHandler;

    public RabbitMqConsumer(IConnection connection, ILogger<RabbitMqConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public void SubscribeToJobCreated(Func<CrawlJobCreated, Task> handler)
    {
        _jobCreatedHandler = handler;
    }

    public void Start()
    {
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: "crawl.events", type: ExchangeType.Topic, durable: true);

        var queueName = _channel.QueueDeclare(queue: "crawl.worker.jobs", durable: true).QueueName;
        _channel.QueueBind(queue: queueName, exchange: "crawl.events", routingKey: "crawljobcreated");

        var dlqName = _channel.QueueDeclare(queue: "crawl.dlq", durable: true).QueueName;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<CrawlJobCreated>(json);

                if (message != null && _jobCreatedHandler != null)
                {
                    await _jobCreatedHandler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, false);
                SendToDeadLetterQueue(ea.Body.ToArray());
            }
        };

        _channel.BasicQos(0, 1, false);
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Worker subscribed to job creation events");
    }

    private void SendToDeadLetterQueue(byte[] body)
    {
        try
        {
            var dlqChannel = _connection.CreateModel();
            dlqChannel.QueueDeclare(queue: "crawl.dlq", durable: true);
            dlqChannel.BasicPublish(exchange: "", routingKey: "crawl.dlq", basicProperties: null, body: body);
            dlqChannel.Close();

            _logger.LogWarning("Message sent to dead letter queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to DLQ");
        }
    }
}
