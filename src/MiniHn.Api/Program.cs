using Microsoft.EntityFrameworkCore;
using MiniHn.Api.Features.Posts;
using MiniHn.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Posts");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fail at start, loudly. A host that boots without a store answers every
    // request with a 500 and looks like a bug in the endpoint.
    throw new InvalidOperationException(
        "ConnectionStrings:Posts is not configured: set it to a PostgreSQL connection string.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// The OpenAPI document and its UI describe the API to a developer; production
// serves the API only.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Persistence must not depend on a step the reader has to be told about, so the
// host creates the database if the server lacks it and applies pending migrations
// before it serves. One process and one small schema make this correct here; a
// multi-instance deployment moves the migrate step into the release pipeline.
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    database.Database.Migrate();
}

app.MapPosts();

app.Run();

// Exposed so WebApplicationFactory<Program> can host this assembly in tests.
public partial class Program;
