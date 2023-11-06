using System.Text.Json;
using Abs.CommonCore.ObservabilityService.Services;
using ObservabilityService.Models;

const string ContainersConfigFile = "Config/AllContainers.json";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var json = await System.IO.File.ReadAllTextAsync(ContainersConfigFile);
var containerInfo = JsonSerializer.Deserialize<MonitorContainer[]>(json)!;
builder.Services.AddSingleton<MonitorContainer[]>(containerInfo);

builder.Services.AddSingleton<IDockerContainerHealthService>(new DockerContainerHealthService());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
