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
}