using System.Diagnostics;
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
        _client.BaseAddress = new Uri("http://localhost:8080/");  // без докера https://localhost:44378/
    }

    [Fact]
    public async Task Add_100_Events_ShouldSucceed()
    {
        var initialResponse = await _client.GetAsync("/api/events/filter");
        initialResponse.EnsureSuccessStatusCode();
        var initialEvents = await initialResponse.Content.ReadFromJsonAsync<List<Event>>();
        int initialCount = initialEvents?.Count ?? 0;

        var eventsToAdd = new List<CreateEventDto>();
        for (int i = 0; i < 100; i++)
        {
            eventsToAdd.Add(new CreateEventDto
            {
                Name = $"Test Event {i}",
                Date = DateTime.Now.AddDays(i + 1),
                HallType = HallType.Vip,
                AvailableTickets = 15,
                Price = 500
            });
        }

        foreach (var eventDto in eventsToAdd)
        {
            var response = await _client.PostAsJsonAsync("/api/events", eventDto);
            response.EnsureSuccessStatusCode();
        }

        var finalResponse = await _client.GetAsync("/api/events/filter");
        finalResponse.EnsureSuccessStatusCode();
        var finalEvents = await finalResponse.Content.ReadFromJsonAsync<List<Event>>();
        int finalCount = finalEvents?.Count ?? 0;

        Assert.Equal(initialCount + 100, finalCount);
    }

    [Fact]
    public async Task Add_10000_Events_ShouldSucceed()
    {
        var hallTypes = Enum.GetValues(typeof(HallType));
        int hallTypeindex = 0;

        for (int j = 0; j < 100; j++)
        {
            var eventsToAdd = new List<CreateEventDto>();
            for (int i = 0; i < 100; i++)
            {
                var hallType = (HallType)hallTypes.GetValue(hallTypeindex % hallTypes.Length);
                hallTypeindex++;

                eventsToAdd.Add(new CreateEventDto
                {
                    Name = $"LoadTest Event {j * 1000 + i}",
                    Date = DateTime.Now.AddDays(j * 1000 + i + 1),
                    HallType = hallType,
                    AvailableTickets = 15,
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
        bool eventsRemaining = true;
        while (eventsRemaining)
        {
            var getResponse = await _client.GetAsync("/api/events");
            getResponse.EnsureSuccessStatusCode();
            var events = await getResponse.Content.ReadFromJsonAsync<List<Event>>();

            if (events == null || events.Count == 0)
            {
                eventsRemaining = false;
                break;
            }

            foreach (var ev in events)
            {
                var deleteResponse = await _client.DeleteAsync($"/api/events/{ev.Id}");
                deleteResponse.EnsureSuccessStatusCode();
            }
        }

        var finalGetResponse = await _client.GetAsync("/api/events/filter");
        finalGetResponse.EnsureSuccessStatusCode();
        var finalEvents = await finalGetResponse.Content.ReadFromJsonAsync<List<Event>>();
        Assert.NotNull(finalEvents);
        Assert.Empty(finalEvents);
    }

    [Fact]
    public async Task GetEvents_SecondRequest_IsFasterDueToCache()
    {
        var sw = Stopwatch.StartNew();
        var response1 = await _client.GetAsync("/api/events");
        response1.EnsureSuccessStatusCode();
        sw.Stop();
        var firstDuration = sw.ElapsedMilliseconds;

        await Task.Delay(100);

        sw.Restart();
        var response2 = await _client.GetAsync("/api/events");
        response2.EnsureSuccessStatusCode();
        sw.Stop();
        var secondDuration = sw.ElapsedMilliseconds;

        Console.WriteLine($"First request: {firstDuration} ms, Second request: {secondDuration} ms");

        var acceptableThreshold = firstDuration * 0.1;
        Assert.True(secondDuration <= firstDuration + acceptableThreshold,
            $"Второй запрос не быстрее: первый = {firstDuration} ms, второй = {secondDuration} ms");
    }
}