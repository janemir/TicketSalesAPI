using Microsoft.AspNetCore.Mvc;
using TicketSalesAPI.Models;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private static List<Event> _events = new List<Event>
    {
        new Event
        {
            Id = 1,
            Name = "Концерт Винтаж",
            Date = new DateTime(2026, 4, 16),
            HallType = HallType.Large,
            AvailableTickets = 150,
            Price = 1850
        },
        new Event
        {
            Id = 2,
            Name = "Экспозиции в залах планетария",
            Date = new DateTime(2026, 2, 28),
            HallType = HallType.Medium,
            AvailableTickets = 48,
            Price = 150
        },
        new Event
        {
            Id = 3,
            Name = "Мертвые души",
            Date = new DateTime(2026, 2, 26),
            HallType = HallType.Small,
            AvailableTickets = 24,
            Price = 1500
        }
    };

    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetEvents() => Ok(_events);

    [HttpGet("{id}")]
    public ActionResult<Event> GetEvent(int id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        return ev == null ? NotFound() : Ok(ev);
    }

    [HttpPost]
    public ActionResult<Event> CreateEvent(CreateEventDto dto)
    {

        if (dto.AvailableTickets > new Event { HallType = dto.HallType }.TotalTickets)
        {
            return BadRequest($"Количество доступных билетов не может превышать вместимость зала ({new Event { HallType = dto.HallType }.TotalTickets})");
        }

        var newEvent = new Event
        {
            Id = _events.Any() ? _events.Max(e => e.Id) + 1 : 1,
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
    public IActionResult UpdateEvent(int id, Event updatedEvent)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null) return NotFound();

        if (updatedEvent.AvailableTickets > new Event { HallType = updatedEvent.HallType }.TotalTickets)
        {
            return BadRequest($"Количество доступных билетов не может превышать вместимость зала ({new Event { HallType = updatedEvent.HallType }.TotalTickets})");
        }

        ev.Name = updatedEvent.Name;
        ev.Date = updatedEvent.Date;
        ev.HallType = updatedEvent.HallType;
        ev.AvailableTickets = updatedEvent.AvailableTickets;
        ev.Price = updatedEvent.Price;

        return Ok(ev);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(int id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null) return NotFound();

        _events.Remove(ev);
        return Ok(_events);
    }
}