using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniHn.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Posts")
    ?? throw new InvalidOperationException("ConnectionStrings:Posts is not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Persistent storage must exist on a fresh clone with no manual step, so the host
// creates the database directory and applies pending migrations before it serves.
// One process and one small schema make this correct at P1; a multi-instance
// deployment moves the migrate step into the release pipeline instead.
EnsureDatabaseDirectoryExists(connectionString);

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    database.Database.Migrate();
}

app.Run();

static void EnsureDatabaseDirectoryExists(string connectionString)
{
    var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
    var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

// Exposed so WebApplicationFactory<Program> can host this assembly in tests.
public partial class Program;
