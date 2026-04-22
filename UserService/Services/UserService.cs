using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserService.Models;

namespace UserService.Services;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserService(IOptions<UserDatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);
        _usersCollection = mongoDatabase.GetCollection<User>(databaseSettings.Value.UsersCollectionName);
    }

    public async Task<List<User>> GetAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<User?> GetAsync(string id) =>
        await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(User newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);

    public async Task IncrementRegisteredObjectsAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update.Inc(x => x.RegisteredObjects, 1);
        await _usersCollection.UpdateOneAsync(filter, update);
    }
}