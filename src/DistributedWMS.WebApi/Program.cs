using DistributedWMS.Infrastructure;
using DistributedWMS.Application;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WmsDb")
    ?? throw new InvalidOperationException("Brak connection stringa 'WmsDb' w konfiguracji.");

builder.Services.AddApplication();

builder.Services.AddInfrastructure(connectionString);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
