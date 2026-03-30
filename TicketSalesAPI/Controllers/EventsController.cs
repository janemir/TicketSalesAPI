using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TicketSalesAPI.Models;
using TicketSalesAPI.Models.Dto;
using TicketSalesAPI.Services;
using Prometheus;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventsService _eventsService;

    private readonly IDistributedCache _cache;

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

    public EventsController(EventsService eventsService, IDistributedCache cache)
    {
        _eventsService = eventsService;
        _cache = cache;
    }

    private async Task InvalidateCache()
    {
        await _cache.RemoveAsync("all_events");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {

        string cacheKey = "all_events";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {

            CacheHits.WithLabels("service-db").Inc();
            var events = JsonSerializer.Deserialize<List<Event>>(cachedData);
            return Ok(events);
        }

        CacheMisses.WithLabels("service-db").Inc();

        var allEvents = await _eventsService.GetAsync();

        var serialized = JsonSerializer.Serialize(allEvents);
        await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

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

        var ev = await _eventsService.GetAsync(id);
        if (ev == null)
            return NotFound();
        return Ok(ev);
    }

    [HttpPost]
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
            Price = dto.Price
        };

        await _eventsService.CreateAsync(newEvent);
        EventsCreatedCounter.WithLabels("service-db").Inc();

        await InvalidateCache();

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
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

        await InvalidateCache();

        return Ok(ev);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var ev = await _eventsService.GetAsync(id);
        if (ev == null) return NotFound();

        await _eventsService.RemoveAsync(id);

        await InvalidateCache();

        return Ok(ev);
    }

    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<Event>>> GetFilteredEvents(
        [FromQuery] string? name,
        [FromQuery] DateTime? fromDate,
        [FromQuery] HallType? hallType)
    {
        string cacheKey = $"filter_{name}_{fromDate?.ToString("yyyyMMdd")}_{hallType}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            CacheHits.WithLabels("service-db").Inc();
            var events = JsonSerializer.Deserialize<List<Event>>(cachedData);
            return Ok(events);
        }

        CacheMisses.WithLabels("service-db").Inc();

        var filteredEvents = await _eventsService.GetFilteredAsync(name, fromDate, hallType);

        var serialized = JsonSerializer.Serialize(filteredEvents);
        await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return Ok(filteredEvents);
    }
}