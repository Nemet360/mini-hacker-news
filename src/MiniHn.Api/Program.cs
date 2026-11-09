var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

// Exposed so WebApplicationFactory<Program> can host this assembly in tests.
public partial class Program;
