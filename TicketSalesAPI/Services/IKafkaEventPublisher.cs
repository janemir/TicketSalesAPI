namespace TicketSalesAPI.Services;

public interface IKafkaEventPublisher
{
    Task ProduceAsync(string topic, object message, CancellationToken cancellationToken = default);
}
