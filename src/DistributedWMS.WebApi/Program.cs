using DistributedWMS.Application;
using DistributedWMS.Infrastructure;
using DistributedWMS.Infrastructure.Persistance;
using DistributedWMS.WebApi.Common;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WmsDb")
    ?? throw new InvalidOperationException("Brak connection stringa 'WmsDb' w konfiguracji.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Dostęp dla aplikacji klienckiej Blazor WebAssembly uruchamianej na innym porcie.
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Zastosowanie migracji oraz zasilenie bazy danymi początkowymi.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DistributedWMS API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.MapControllers();

app.Run();
