using UserService.Models;

namespace UserService.Models.Dto;

public sealed class UserResponseDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegisteredObjects { get; set; }

    public static UserResponseDto FromUser(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        RegisteredObjects = user.RegisteredObjects
    };
}
