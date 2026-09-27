using HotChocolate.Types;
using UserService.Models;

namespace UserService.GraphQL;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(u => u.PasswordHash).Ignore();
    }
}