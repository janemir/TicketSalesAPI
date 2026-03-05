using System.ComponentModel.DataAnnotations;

namespace TicketSalesAPI.Models;

public class CreateEventDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [FutureDate(ErrorMessage = "Дата мероприятия не может быть в прошлом")]
    public DateTime Date { get; set; }

    [Required]
    public HallType HallType { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Количество билетов не может быть отрицательным")]
    public int AvailableTickets { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
    public decimal Price { get; set; }
}