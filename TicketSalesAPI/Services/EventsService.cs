using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TicketSalesAPI.Models;

namespace TicketSalesAPI.Services;

public class EventsService
{
    private readonly IMongoCollection<Event> _eventsCollection;

    public EventsService(IOptions<EventStoreDBSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);
        _eventsCollection = mongoDatabase.GetCollection<Event>(databaseSettings.Value.EventsCollectionName);
    }

    public async Task<List<Event>> GetAsync() =>
        await _eventsCollection.Find(_ => true).ToListAsync();

    public async Task<Event?> GetAsync(string id)
    {
        try
        {
            return await _eventsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public async Task CreateAsync(Event newEvent) =>
        await _eventsCollection.InsertOneAsync(newEvent);

    public async Task UpdateAsync(string id, Event updatedEvent) =>
        await _eventsCollection.ReplaceOneAsync(x => x.Id == id, updatedEvent);

    public async Task RemoveAsync(string id) =>
        await _eventsCollection.DeleteOneAsync(x => x.Id == id);

    public async Task<List<Event>> GetRandomAsync(int sampleSize)
    {
        return await _eventsCollection.Aggregate()
                                      .Sample(sampleSize)
                                      .ToListAsync();
    }

    public async Task<List<Event>> GetFilteredAsync(string? name, DateTime? fromDate, HallType? hallType)
    {
        var builder = Builders<Event>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(name))
            filter &= builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(name, "i"));

        if (fromDate.HasValue)
            filter &= builder.Gte(x => x.Date, fromDate.Value);

        if (hallType.HasValue)
            filter &= builder.Eq(x => x.HallType, hallType.Value);

        return await _eventsCollection.Find(filter).ToListAsync();
    }
}