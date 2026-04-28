using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TicketSalesAPI.Models;
using TicketSalesAPI.Models.Dto;
using TicketSalesAPI.Services;
using Prometheus;
using Microsoft.AspNetCore.Authorization;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{

    private readonly EventsService _eventsService;
    private readonly IDistributedCache _cache;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly ILogger<EventsController> _logger;

    private static readonly Counter EventsCreatedCounter = Metrics.CreateCounter(
        "events_created_total",
        "Total number of events created",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    private static readonly Counter EventsValidationErrorsCounter = Metrics.CreateCounter(
        "events_validation_errors_total",
        "Total number of validation errors (tickets > capacity)",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    private static readonly Counter CacheHits = Metrics.CreateCounter(
        "cache_hits_total",
        "Total number of cache hits",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    private static readonly Counter CacheMisses = Metrics.CreateCounter(
        "cache_misses_total",
        "Total number of cache misses",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    private static readonly Histogram CacheOperationDuration = Metrics.CreateHistogram(
        "cache_operation_duration_seconds",
        "Duration of Redis cache operations (read/write/delete)",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service", "operation" }
        });

    public EventsController(
        EventsService eventsService,
        IDistributedCache cache,
        KafkaProducerService kafkaProducer,
        ILogger<EventsController> logger)
    {
        _eventsService = eventsService;
        _cache = cache;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    private async Task<string> GetCurrentCacheVersionAsync()
    {
        const string versionKey = "cache_version";
        var version = await _cache.GetStringAsync(versionKey);
        if (string.IsNullOrEmpty(version))
        {
            version = "1";
            await _cache.SetStringAsync(versionKey, version, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            });
        }
        return version;
    }

    private async Task IncrementCacheVersionAsync()
    {
        const string versionKey = "cache_version";
        var version = await _cache.GetStringAsync(versionKey);
        int newVersion = 1;
        if (!string.IsNullOrEmpty(version) && int.TryParse(version, out int current))
            newVersion = current + 1;
        await _cache.SetStringAsync(versionKey, newVersion.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
        });
    }

    private async Task InvalidateCache(string? eventId = null)
    {
        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "delete").NewTimer())
            {
                await _cache.RemoveAsync("all_events");
            }

            if (!string.IsNullOrEmpty(eventId))
            {
                using (CacheOperationDuration.WithLabels("service-db", "delete").NewTimer())
                {
                    await _cache.RemoveAsync($"event_{eventId}");
                }
            }

            await IncrementCacheVersionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, инвалидировать кэш не удалось");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        string cacheKey = "all_events";
        string? cachedData = null;

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "read").NewTimer())
            {
                cachedData = await _cache.GetStringAsync(cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, чтение из кэша пропущено");
        }

        if (!string.IsNullOrEmpty(cachedData))
        {
            CacheHits.WithLabels("service-db").Inc();
            var events = JsonSerializer.Deserialize<List<Event>>(cachedData);
            return Ok(events);
        }

        CacheMisses.WithLabels("service-db").Inc();
        var allEvents = await _eventsService.GetAsync();

        var serialized = JsonSerializer.Serialize(allEvents);

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "write").NewTimer())
            {
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, запись в кэш пропущена");
        }

        if (allEvents.Count > 1000)
        {
            var randomEvents = await _eventsService.GetRandomAsync(1000);
            return Ok(randomEvents);
        }
        return Ok(allEvents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(string id)
    {
        string cacheKey = $"event_{id}";
        string? cachedData = null;

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "read").NewTimer())
            {
                cachedData = await _cache.GetStringAsync(cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, чтение из кэша пропущено");
        }

        if (!string.IsNullOrEmpty(cachedData))
        {
            CacheHits.WithLabels("service-db").Inc();
            var ev = JsonSerializer.Deserialize<Event>(cachedData);
            return Ok(ev);
        }

        CacheMisses.WithLabels("service-db").Inc();
        var evFromDb = await _eventsService.GetAsync(id);
        if (evFromDb == null)
            return NotFound();

        var serialized = JsonSerializer.Serialize(evFromDb);

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "write").NewTimer())
            {
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, запись в кэш пропущена");
        }

        return Ok(evFromDb);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Event>> CreateEvent(CreateEventDto dto)
    {
        if (dto.AvailableTickets > new Event { HallType = dto.HallType }.TotalTickets)
        {
            EventsValidationErrorsCounter.WithLabels("service-db").Inc();
            return BadRequest($"Количество доступных билетов не может превышать вместимость зала ({new Event { HallType = dto.HallType }.TotalTickets})");
        }

        var newEvent = new Event
        {
            Name = dto.Name,
            Date = dto.Date,
            HallType = dto.HallType,
            AvailableTickets = dto.AvailableTickets,
            Price = dto.Price,
            UserId = dto.UserId,
            ConfirmationStatus = "Pending"
        };

        await _eventsService.CreateAsync(newEvent);
        EventsCreatedCounter.WithLabels("service-db").Inc();

        await _kafkaProducer.ProduceAsync("object-created-topic", new
        {
            ObjectId = newEvent.Id,
            UserId = dto.UserId
        });

        await InvalidateCache();

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateEvent(string id, CreateEventDto dto)
    {
        var ev = await _eventsService.GetAsync(id);
        if (ev == null) return NotFound();

        if (dto.AvailableTickets > new Event { HallType = dto.HallType }.TotalTickets)
        {
            EventsValidationErrorsCounter.WithLabels("service-db").Inc();
            return BadRequest($"Количество доступных билетов не может превышать вместимость зала ({new Event { HallType = dto.HallType }.TotalTickets})");
        }

        ev.Name = dto.Name;
        ev.Date = dto.Date;
        ev.HallType = dto.HallType;
        ev.AvailableTickets = dto.AvailableTickets;
        ev.Price = dto.Price;

        await _eventsService.UpdateAsync(id, ev);
        await InvalidateCache(id);

        return Ok(ev);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var ev = await _eventsService.GetAsync(id);
        if (ev == null) return NotFound();

        await _eventsService.RemoveAsync(id);
        await InvalidateCache(id);

        return Ok(ev);
    }

    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<Event>>> GetFilteredEvents(
        [FromQuery] string? name,
        [FromQuery] DateTime? fromDate,
        [FromQuery] HallType? hallType)
    {
        string version = await GetCurrentCacheVersionAsync();
        string cacheKey = $"filter_v{version}_{name}_{fromDate?.ToString("yyyyMMdd")}_{hallType}";
        string? cachedData = null;

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "read").NewTimer())
            {
                cachedData = await _cache.GetStringAsync(cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, чтение из кэша пропущено");
        }

        if (!string.IsNullOrEmpty(cachedData))
        {
            CacheHits.WithLabels("service-db").Inc();
            var events = JsonSerializer.Deserialize<List<Event>>(cachedData);
            return Ok(events);
        }

        CacheMisses.WithLabels("service-db").Inc();
        var filteredEvents = await _eventsService.GetFilteredAsync(name, fromDate, hallType);
        var serialized = JsonSerializer.Serialize(filteredEvents);

        try
        {
            using (CacheOperationDuration.WithLabels("service-db", "write").NewTimer())
            {
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен, запись в кэш пропущена");
        }

        return Ok(filteredEvents);
    }
}