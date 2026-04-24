using Confluent.Kafka;
using System.Text.Json;
using TicketSalesAPI.Services;

namespace TicketSalesAPI.Services;

public class ConfirmationConsumerService : BackgroundService
{
    private readonly ILogger<ConfirmationConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public ConfirmationConsumerService(ILogger<ConfirmationConsumerService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "ticketsales-confirmation-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe("confirmation-topic");
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

                _logger.LogInformation($"Получено подтверждение: {consumeResult.Message.Value}");
                var confirmation = JsonSerializer.Deserialize<ConfirmationMessage>(consumeResult.Message.Value);
                if (confirmation != null)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var eventsService = scope.ServiceProvider.GetRequiredService<EventsService>();
                        var ev = await eventsService.GetAsync(confirmation.ObjectId);
                        if (ev != null)
                        {
                            ev.ConfirmationStatus = "Confirmed";
                            ev.ConfirmationTime = DateTime.Parse(confirmation.ConfirmationTime);
                            await eventsService.UpdateAsync(confirmation.ObjectId, ev);
                            _logger.LogInformation($"Обновлён статус для объекта {confirmation.ObjectId}: Confirmed at {confirmation.ConfirmationTime}");
                        }
                        else
                        {
                            _logger.LogWarning($"Объект с ID {confirmation.ObjectId} не найден");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке подтверждения");
                await Task.Delay(500, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class ConfirmationMessage
{
    public string ObjectId { get; set; } = string.Empty;
    public string ConfirmationTime { get; set; } = string.Empty;
}