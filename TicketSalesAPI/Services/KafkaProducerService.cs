using Confluent.Kafka;
using System.Text.Json;

namespace TicketSalesAPI.Services;

public class KafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, object message)
    {
        var serialized = JsonSerializer.Serialize(message);
        try
        {
            var deliveryResult = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = serialized });
            _logger.LogInformation($"Сообщение отправлено в топик {topic}: {serialized}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, $"Ошибка отправки: {ex.Error.Reason}");
            throw;
        }
    }
}