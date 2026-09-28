using HotChocolate.Types;
using TicketSalesAPI.Models;

namespace TicketSalesAPI.GraphQL;

public class EventType : ObjectType<Event>
{
    protected override void Configure(IObjectTypeDescriptor<Event> descriptor)
    {
        descriptor.Field(e => e.TotalTickets).Type<IntType>();
    }
}