using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace UserService.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Имя пользователя (логин для аутентификации).</summary>
    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("PasswordHash")]
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("RegisteredObjects")]
    public int RegisteredObjects { get; set; } = 0;
}