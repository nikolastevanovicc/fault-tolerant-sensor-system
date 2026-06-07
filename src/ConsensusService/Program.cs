using ConsensusService;
using ConsensusService.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IConsensusProcessor, ConsensusProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
