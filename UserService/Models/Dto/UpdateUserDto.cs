using System.ComponentModel.DataAnnotations;

namespace UserService.Models.Dto;

public class UpdateUserDto
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string? Password { get; set; }
}
