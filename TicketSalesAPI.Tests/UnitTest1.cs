using System.Net.Http.Json;
using TicketSalesAPI.Models;
using TicketSalesAPI.Models.Dto;

namespace TicketSalesAPI.Tests;

public class ApiTests
{
    private readonly HttpClient _client;

    public ApiTests()
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://localhost:44378/");
    }

    [Fact]
    public async Task Add_100_Events_ShouldSucceed()
    {
        var eventsToAdd = new List<CreateEventDto>();
        for (int i = 0; i < 100; i++)
        {
            eventsToAdd.Add(new CreateEventDto
            {
                Name = $"Test Event {i}",
                Date = DateTime.Now.AddDays(i),
                HallType = HallType.Medium,
                AvailableTickets = 100,
                Price = 500
            });
        }

        foreach (var eventDto in eventsToAdd)
        {
            var response = await _client.PostAsJsonAsync("/api/events", eventDto);
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Add_100000_Events_ShouldSucceed()
    {
        for (int j = 0; j < 1000; j++)
        {
            var eventsToAdd = new List<CreateEventDto>();
            for (int i = 0; i < 100; i++)
            {
                eventsToAdd.Add(new CreateEventDto
                {
                    Name = $"LoadTest Event {j * 1000 + i}",
                    Date = DateTime.Now.AddDays(j * 1000 + i),
                    HallType = HallType.Small,
                    AvailableTickets = 50,
                    Price = 200
                });
            }

            foreach (var eventDto in eventsToAdd)
            {
                var response = await _client.PostAsJsonAsync("/api/events", eventDto);
                response.EnsureSuccessStatusCode();
            }
        }
    }

    [Fact]
    public async Task Delete_All_Events_ShouldSucceed()
    {
        var getResponse = await _client.GetAsync("/api/events");
        getResponse.EnsureSuccessStatusCode();

        var events = await getResponse.Content.ReadFromJsonAsync<List<Event>>();

        if (events != null)
        {
            foreach (var ev in events)
            {
                var deleteResponse = await _client.DeleteAsync($"/api/events/{ev.Id}");
                deleteResponse.EnsureSuccessStatusCode();
            }
        }
    }
}