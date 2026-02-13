namespace TicketSalesAPI.Models;

public class EventsController
{
    private static List<Event> _events = new List<Event>
    {
        new Event { Id = 1, Name = "Концерт Винтаж", Date = new DateTime(2026, 4, 16), Venue = "Киноконцертный зал", AvailableTickets = 150, Price = 1850 },
        new Event { Id = 2, Name = "Экспозиции в залах планетария", Date = new DateTime(2026, 2, 28), Venue = "Планетарий", AvailableTickets = 80, Price = 150 },
        new Event { Id = 3, Name = "Мертвые души", Date = new DateTime(2026, 2, 26), Venue = "Драматический театр им. Луначарского", AvailableTickets = 45, Price = 1500 }
    };

    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetEvents()
    {
        return Ok(_events);
    }

    [HttpGet("{id}")]
    public ActionResult<Event> GetEvent(int id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null)
            return NotFound();
        return Ok(ev);
    }

    [HttpPost]
    public ActionResult<Event> CreateEvent(Event newEvent)
    {
        newEvent.Id = _events.Max(e => e.Id) + 1;
        _events.Add(newEvent);

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
    }
}