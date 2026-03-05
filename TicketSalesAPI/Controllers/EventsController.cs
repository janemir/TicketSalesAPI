using Microsoft.AspNetCore.Mvc;
using TicketSalesAPI.Models;
using TicketSalesAPI.Models.Dto;
using TicketSalesAPI.Services;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventsService _eventsService;

    public EventsController(EventsService eventsService)
    {
        _eventsService = eventsService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        var allEvents = await _eventsService.GetAsync();
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
        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, Event updatedEvent)
    {
        var ev = await _eventsService.GetAsync(id);
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

        await _eventsService.UpdateAsync(id, ev);
        return Ok(ev);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var ev = await _eventsService.GetAsync(id);
        if (ev == null) return NotFound();

        await _eventsService.RemoveAsync(id);
        return NoContent();
    }

    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<Event>>> GetFilteredEvents(
        [FromQuery] string? name,
        [FromQuery] DateTime? fromDate,
        [FromQuery] HallType? hallType)
    {
        var filteredEvents = await _eventsService.GetFilteredAsync(name, fromDate, hallType);
        return Ok(filteredEvents);
    }
}