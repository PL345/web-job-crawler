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
        _logger.LogInformation("Subscribed to job created with handler");
    }

    public void Start()
    {
        try
        {
            _logger.LogInformation("Starting RabbitMQ consumer");
            _channel = _connection.CreateModel();
            _logger.LogInformation("Channel created");

            _channel.ExchangeDeclare(exchange: "crawl.events", type: ExchangeType.Topic, durable: true);
            _logger.LogInformation("Exchange declared");

            var queueName = _channel.QueueDeclare(queue: "crawl.worker.jobs", durable: true).QueueName;
            _logger.LogInformation("Queue declared: {QueueName}", queueName);
            
            _channel.QueueBind(queue: queueName, exchange: "crawl.events", routingKey: "crawljobcreated");
            _logger.LogInformation("Queue bound to exchange with routing key crawljobcreated");

            var dlqName = _channel.QueueDeclare(queue: "crawl.dlq", durable: true).QueueName;

            var consumer = new EventingBasicConsumer(_channel);
            _logger.LogInformation("Consumer created");
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    _logger.LogInformation("Message received from queue");
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    _logger.LogDebug("Message content: {MessageContent}", json);
                    var message = JsonConvert.DeserializeObject<CrawlJobCreated>(json);

                    if (message != null && _jobCreatedHandler != null)
                    {
                        _logger.LogInformation("Processing message with JobId {JobId}", message.JobId);
                        await _jobCreatedHandler(message);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Message acknowledged for JobId {JobId}", message.JobId);
                    }
                    else
                    {
                        _logger.LogWarning("Message deserialization failed or handler is null");
                        _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message, sending to DLQ");
                    try
                    {
                        _channel.BasicNack(ea.DeliveryTag, false, false); // Don't requeue
                    }
                    catch { }
                    SendToDeadLetterQueue(ea.Body.ToArray());
                }
            };

        _channel.BasicQos(0, 1, false);
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Worker subscribed to job creation events on queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RabbitMQ consumer");
            throw;
        }
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
