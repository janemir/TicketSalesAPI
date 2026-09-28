using TicketSalesAPI.Models;
using TicketSalesAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TicketSalesAPI.GraphQL;

public class Mutation
{
    private async Task InvalidateCache(IDistributedCache cache, string? eventId = null)
    {
        await cache.RemoveAsync("all_events");
        if (!string.IsNullOrEmpty(eventId))
        {
            await cache.RemoveAsync($"event_{eventId}");
        }
    }

    public async Task<Event> CreateEvent(
        string name,
        DateTime date,
        HallType hallType,
        int availableTickets,
        decimal price,
        string userId,
        [Service] IEventsService eventsService,
        [Service] IKafkaEventPublisher kafkaProducer,
        [Service] IDistributedCache cache)
    {
        var newEvent = new Event
        {
            Name = name,
            Date = date,
            HallType = hallType,
            AvailableTickets = availableTickets,
            Price = price,
            UserId = userId,
            ConfirmationStatus = "Pending"
        };

        await eventsService.CreateAsync(newEvent);

        await kafkaProducer.ProduceAsync("object-created-topic", new
        {
            ObjectId = newEvent.Id,
            UserId = userId
        });

        await InvalidateCache(cache);
        return newEvent;
    }

    public async Task<Event?> UpdateEvent(
        string id,
        string name,
        DateTime date,
        HallType hallType,
        int availableTickets,
        decimal price,
        string userId,
        [Service] IEventsService eventsService,
        [Service] IDistributedCache cache)
    {
        var ev = await eventsService.GetAsync(id);
        if (ev == null) return null;

        ev.Name = name;
        ev.Date = date;
        ev.HallType = hallType;
        ev.AvailableTickets = availableTickets;
        ev.Price = price;
        ev.UserId = userId;

        await eventsService.UpdateAsync(id, ev);
        await InvalidateCache(cache, id);
        return ev;
    }

    public async Task<bool> DeleteEvent(
        string id,
        [Service] IEventsService eventsService,
        [Service] IDistributedCache cache)
    {
        var ev = await eventsService.GetAsync(id);
        if (ev == null) return false;

        await eventsService.RemoveAsync(id);
        await InvalidateCache(cache, id);
        return true;
    }
}