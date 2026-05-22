using System.ComponentModel.DataAnnotations;

namespace UserService.Models.Dto;

public sealed class ValidateCredentialsRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
