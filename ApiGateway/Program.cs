using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "TicketSales.Auth";
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "TicketSales.Api";
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("Jwt:Key is required");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "ApiGateway" }));

await app.UseOcelot();

app.Run();
