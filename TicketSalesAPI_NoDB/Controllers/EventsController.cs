using Microsoft.AspNetCore.Mvc;
using TicketSalesAPI.Models;
using TicketSalesAPI.Models.Dto;
using Prometheus;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private static List<Event> _events = new List<Event>
    {
        new Event { Id = "1", Name = "Концерт Винтаж", Date = new DateTime(2026, 4, 16), HallType = HallType.Large, AvailableTickets = 150, Price = 1850 },
        new Event { Id = "2", Name = "Экспозиции в залах планетария", Date = new DateTime(2026, 2, 28), HallType = HallType.Medium, AvailableTickets = 48, Price = 150 },
        new Event { Id = "3", Name = "Мертвые души", Date = new DateTime(2026, 2, 26), HallType = HallType.Small, AvailableTickets = 24, Price = 1500 }
    };

    private static readonly Counter EventsCreatedCounter = Metrics.CreateCounter(
        "events_created_total",
        "Total number of events created",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    private static readonly Counter EventsValidationErrorsCounter = Metrics.CreateCounter(
        "events_validation_errors_total",
        "Total number of validation errors (tickets > capacity)",
        new CounterConfiguration { LabelNames = new[] { "service" } });

    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetEvents() => Ok(_events);

    [HttpGet("{id}")]
    public ActionResult<Event> GetEvent(string id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        return ev == null ? NotFound() : Ok(ev);
    }

    [HttpPost]
    public ActionResult<Event> CreateEvent(CreateEventDto dto)
    {
        if (dto.AvailableTickets > new Event { HallType = dto.HallType }.TotalTickets)
            return BadRequest($"Количество билетов превышает вместимость зала ({new Event { HallType = dto.HallType }.TotalTickets})");

        var newEvent = new Event
        {
            Id = (_events.Any() ? int.Parse(_events.Max(e => e.Id)) + 1 : 1).ToString(),
            Name = dto.Name,
            Date = dto.Date,
            HallType = dto.HallType,
            AvailableTickets = dto.AvailableTickets,
            Price = dto.Price
        };
        _events.Add(newEvent);
        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateEvent(string id, CreateEventDto dto)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null) return NotFound();

        if (dto.AvailableTickets > new Event { HallType = dto.HallType }.TotalTickets)
            return BadRequest($"Количество билетов превышает вместимость зала ({new Event { HallType = dto.HallType }.TotalTickets})");

        ev.Name = dto.Name;
        ev.Date = dto.Date;
        ev.HallType = dto.HallType;
        ev.AvailableTickets = dto.AvailableTickets;
        ev.Price = dto.Price;

        return Ok(ev);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(string id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null) return NotFound();
        _events.Remove(ev);
        return Ok(ev);
    }

    [HttpGet("filter")]
    public ActionResult<IEnumerable<Event>> GetFilteredEvents(
        [FromQuery] string? name,
        [FromQuery] DateTime? fromDate,
        [FromQuery] HallType? hallType)
    {
        var filtered = _events.AsEnumerable();
        if (!string.IsNullOrEmpty(name))
            filtered = filtered.Where(e => e.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (fromDate.HasValue)
            filtered = filtered.Where(e => e.Date >= fromDate.Value);
        if (hallType.HasValue)
            filtered = filtered.Where(e => e.HallType == hallType.Value);
        return Ok(filtered.ToList());
    }
}