using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Infrastructure.Authentication;
using DistributedWMS.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedWMS.Infrastructure;

///<summary>
///Rejestracja usług warstwy infrastruktury. Warstwa WebAPI wywołuje
///wyłącznie tę metodę i nie zna szczegółów dostępu do bazy danych.
/// </summary>
public static class DepencencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WmsDbContext>(options =>
            options.UseNpgsql( connectionString));

        services.AddScoped<IWmsDbContext>(sp => sp.GetRequiredService<WmsDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUser, CurrentUser>();


        return services;
    }
}