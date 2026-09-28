using HotChocolate.Data;
using UserService.Models;

namespace UserService.GraphQL;

public class Query
{

    [UseFiltering]
    [UseSorting]
    public async Task<List<User>> GetUsers([Service] Services.UserService userService)
        => await userService.GetAsync();

    public async Task<User?> GetUser(string id, [Service] Services.UserService userService)
        => await userService.GetAsync(id);
}