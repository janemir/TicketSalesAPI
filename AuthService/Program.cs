using AuthService.Services;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService", Version = "v1" });
});

builder.Services.AddHttpClient("UserService")
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var userServiceBaseUrl = configuration.GetValue<string>("UserService:BaseUrl")
            ?? "http://localhost:8082";
        client.BaseAddress = new Uri(userServiceBaseUrl.TrimEnd('/') + "/");
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue("HttpsRedirection:Enabled", false))
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.UseHttpMetrics();
app.MapMetrics();

app.Run();
