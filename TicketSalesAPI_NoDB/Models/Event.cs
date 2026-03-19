namespace TicketSalesAPI.Models;

public class Event
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public HallType HallType { get; set; }
    public int AvailableTickets { get; set; }
    public decimal Price { get; set; }

    public int TotalTickets => HallType switch
    {
        HallType.Small => 30,
        HallType.Medium => 60,
        HallType.Large => 100,
        HallType.Vip => 20,
        _ => 0
    };
}