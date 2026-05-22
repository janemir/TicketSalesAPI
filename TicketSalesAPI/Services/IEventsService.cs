using TicketSalesAPI.Models;

namespace TicketSalesAPI.Services;

public interface IEventsService
{
    Task<List<Event>> GetAsync();
    Task<Event?> GetAsync(string id);
    Task CreateAsync(Event newEvent);
    Task UpdateAsync(string id, Event updatedEvent);
    Task RemoveAsync(string id);
    Task<List<Event>> GetRandomAsync(int sampleSize);
    Task<List<Event>> GetFilteredAsync(string? name, DateTime? fromDate, HallType? hallType);
}
