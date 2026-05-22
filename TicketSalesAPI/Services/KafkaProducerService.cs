using System.Text.Json;
using Confluent.Kafka;

namespace TicketSalesAPI.Services;

public sealed class KafkaProducerService : IKafkaEventPublisher
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(ILogger<KafkaProducerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, object message, CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(message);
        try
        {
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = serialized }, cancellationToken);
            _logger.LogInformation("Сообщение отправлено в топик {Topic}: {Payload}", topic, serialized);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Ошибка отправки в Kafka: {Reason}", ex.Error.Reason);
            throw;
        }
    }
}
