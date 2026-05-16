using Microsoft.AspNetCore.Identity;
using UserService.Models;

namespace UserService.Services;

public sealed class PasswordHashingService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password) =>
        _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result != PasswordVerificationResult.Failed;
    }
}
