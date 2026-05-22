using System.Text.Json;
using Confluent.Kafka;

namespace TicketSalesAPI.Services;

public sealed class ConfirmationConsumerService : BackgroundService
{
    private readonly ILogger<ConfirmationConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public ConfirmationConsumerService(
        ILogger<ConfirmationConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
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

                _logger.LogInformation("Получено подтверждение: {Message}", consumeResult.Message.Value);
                var confirmation = JsonSerializer.Deserialize<ConfirmationMessage>(consumeResult.Message.Value);
                if (confirmation == null || string.IsNullOrWhiteSpace(confirmation.ObjectId))
                    continue;

                using var scope = _scopeFactory.CreateScope();
                        var eventsService = scope.ServiceProvider.GetRequiredService<IEventsService>();
                var ev = await eventsService.GetAsync(confirmation.ObjectId);
                if (ev == null)
                {
                    _logger.LogWarning("Объект с ID {ObjectId} не найден", confirmation.ObjectId);
                    continue;
                }

                ev.ConfirmationStatus = "Confirmed";
                if (DateTime.TryParse(confirmation.ConfirmationTime, out var confirmedAt))
                    ev.ConfirmationTime = confirmedAt;
                else
                    ev.ConfirmationTime = DateTime.UtcNow;

                await eventsService.UpdateAsync(confirmation.ObjectId, ev);
                _logger.LogInformation(
                    "Обновлён статус для объекта {ObjectId}: Confirmed at {Time}",
                    confirmation.ObjectId,
                    ev.ConfirmationTime);
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
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}

public sealed class ConfirmationMessage
{
    public string ObjectId { get; set; } = string.Empty;
    public string ConfirmationTime { get; set; } = string.Empty;
}
