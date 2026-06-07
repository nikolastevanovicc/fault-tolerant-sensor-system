using IngestionService.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddScoped<IReadingPersistence, PostgresReadingPersistence>();
builder.Services.AddSingleton<ISensorStateStore, SensorStateStore>();
builder.Services.AddSingleton<AlarmConsoleWriter>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
