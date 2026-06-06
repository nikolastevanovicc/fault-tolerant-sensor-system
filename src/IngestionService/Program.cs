using IngestionService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ISensorStateStore, SensorStateStore>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
