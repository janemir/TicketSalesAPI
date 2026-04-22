using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketSalesAPI.Models;

public class Event
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("Date")]
    public DateTime Date { get; set; }

    [BsonElement("HallType")]
    public HallType HallType { get; set; }

    [BsonElement("AvailableTickets")]
    public int AvailableTickets { get; set; }

    [BsonElement("Price")]
    public decimal Price { get; set; }

    [BsonElement("UserId")]
    public string? UserId { get; set; }

    [BsonElement("ConfirmationStatus")]
    public string? ConfirmationStatus { get; set; }

    public int TotalTickets => HallType switch
    {
        HallType.Small => 30,
        HallType.Medium => 60,
        HallType.Large => 100,
        HallType.Vip => 20,
        _ => 0
    };
}