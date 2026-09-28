using TicketSalesAPI.Models;

namespace TicketSalesAPI.GraphQL;

public class Query
{
    [UseMongoDBFiltering]
    [UseMongoDBSorting]
    public async Task<List<Event>> GetEvents([Service] Services.IEventsService eventsService)
        => await eventsService.GetAsync();

    public async Task<Event?> GetEvent(string id, [Service] Services.IEventsService eventsService)
        => await eventsService.GetAsync(id);

    public async Task<List<Event>> GetFilteredEvents(
        string? name,
        DateTime? fromDate,
        HallType? hallType,
        [Service] Services.IEventsService eventsService)
        => await eventsService.GetFilteredAsync(name, fromDate, hallType);
}