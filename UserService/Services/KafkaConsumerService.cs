using Confluent.Kafka;
using System.Text.Json;

namespace UserService.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly IProducer<Null, string> _producer;

    public KafkaConsumerService(ILogger<KafkaConsumerService> logger)
    {
        _logger = logger;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "user-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        _consumer.Subscribe("object-created-topic");

        var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    _logger.LogInformation($"Получено сообщение: {consumeResult.Message.Value}");
                    var message = JsonSerializer.Deserialize<Dictionary<string, object>>(consumeResult.Message.Value);
                    var objectId = message?["ObjectId"]?.ToString();

                    if (!string.IsNullOrEmpty(objectId))
                    {
                        // Эмуляция проверки
                        await Task.Delay(500, stoppingToken);
                        var confirmation = new
                        {
                            ObjectId = objectId,
                            ConfirmationTime = DateTime.UtcNow,
                            Status = "Confirmed",
                            UserId = "user-123"
                        };
                        var serialized = JsonSerializer.Serialize(confirmation);
                        await _producer.ProduceAsync("confirmation-topic", new Message<Null, string> { Value = serialized }, stoppingToken);
                        _logger.LogInformation($"Отправлено подтверждение для объекта {objectId}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения");
            }
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        _producer?.Dispose();
        base.Dispose();
    }
}