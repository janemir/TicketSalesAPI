using UserService.Models;

namespace UserService.GraphQL;

public class Mutation
{
    public async Task<User> CreateUser(
        string name,
        string password,
        [Service] Services.UserService userService)
    {
        return await userService.CreateWithPasswordAsync(name, password);
    }

    public async Task<User?> UpdateUser(
        string id,
        string name,
        string? password,
        [Service] Services.UserService userService)
    {
        return await userService.UpdateProfileAsync(id, name, password);
    }

    public async Task<bool> DeleteUser(
        string id,
        [Service] Services.UserService userService)
    {
        var user = await userService.GetAsync(id);
        if (user == null) return false;
        await userService.RemoveAsync(id);
        return true;
    }

    public async Task<User?> ValidateCredentials(
        string name,
        string password,
        [Service] Services.UserService userService)
    {
        return await userService.ValidateCredentialsAsync(name, password);
    }
}