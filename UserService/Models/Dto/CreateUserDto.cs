using System.ComponentModel.DataAnnotations;

namespace UserService.Models.Dto;

public class CreateUserDto
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
    public string Name { get; set; } = string.Empty;
}