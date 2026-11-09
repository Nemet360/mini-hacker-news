using Microsoft.EntityFrameworkCore;
using MiniHn.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Posts")
                      ?? throw new InvalidOperationException("ConnectionStrings:Posts is not configured.")));

var app = builder.Build();

app.Run();

// Exposed so WebApplicationFactory<Program> can host this assembly in tests.
public partial class Program;
