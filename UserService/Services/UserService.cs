using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserService.Models;

namespace UserService.Services;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly PasswordHashingService _passwordHashing;

    public UserService(IOptions<UserDatabaseSettings> databaseSettings, PasswordHashingService passwordHashing)
    {
        _passwordHashing = passwordHashing;
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);
        _usersCollection = mongoDatabase.GetCollection<User>(databaseSettings.Value.UsersCollectionName);
    }

    public async Task<List<User>> GetAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<User?> GetAsync(string id) =>
        await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<User?> GetByNameAsync(string name) =>
        await _usersCollection.Find(x => x.Name == name).FirstOrDefaultAsync();

    public async Task<User> CreateWithPasswordAsync(string name, string password)
    {
        var user = new User
        {
            Name = name,
            RegisteredObjects = 0
        };
        user.PasswordHash = _passwordHashing.HashPassword(user, password);
        await _usersCollection.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> ValidateCredentialsAsync(string name, string password)
    {
        var user = await GetByNameAsync(name);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        return _passwordHashing.VerifyPassword(user, password) ? user : null;
    }

    public async Task CreateAsync(User newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task<User?> UpdateProfileAsync(string id, string name, string? newPassword)
    {
        var user = await GetAsync(id);
        if (user == null) return null;

        user.Name = name;
        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = _passwordHashing.HashPassword(user, newPassword);

        await UpdateAsync(id, user);
        return user;
    }

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);

    public async Task IncrementRegisteredObjectsAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update.Inc(x => x.RegisteredObjects, 1);
        await _usersCollection.UpdateOneAsync(filter, update);
    }
}