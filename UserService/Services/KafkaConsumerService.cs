using System.Text.Json;
using Confluent.Kafka;

namespace UserService.Services;

public sealed class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly IProducer<Null, string> _producer;
    private readonly IServiceScopeFactory _scopeFactory;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "user-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        _consumer.Subscribe("object-created-topic");

        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(500));
                if (consumeResult == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Получено сообщение: {Message}", consumeResult.Message.Value);
                var message = JsonSerializer.Deserialize<Dictionary<string, string>>(consumeResult.Message.Value);
                if (message == null ||
                    !message.TryGetValue("ObjectId", out var objectId) ||
                    !message.TryGetValue("UserId", out var userId) ||
                    string.IsNullOrWhiteSpace(objectId) ||
                    string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Сообщение не содержит ObjectId или UserId");
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<Services.UserService>();
                var user = await userService.GetAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь с ID {UserId} не найден", userId);
                    continue;
                }

                await userService.IncrementRegisteredObjectsAsync(userId);
                _logger.LogInformation("Инкрементирован RegisteredObjects для пользователя {UserId}", userId);

                var response = new
                {
                    ObjectId = objectId,
                    ConfirmationTime = DateTime.UtcNow.ToString("o")
                };
                var serializedResponse = JsonSerializer.Serialize(response);
                await _producer.ProduceAsync(
                    "confirmation-topic",
                    new Message<Null, string> { Value = serializedResponse },
                    stoppingToken);
                _logger.LogInformation("Отправлено подтверждение для объекта {ObjectId}", objectId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения Kafka");
                await Task.Delay(500, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        _producer.Dispose();
        base.Dispose();
    }
}
