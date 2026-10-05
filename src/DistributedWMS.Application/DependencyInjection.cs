using DistributedWMS.Application.Locations;
using DistributedWMS.Application.Products;
using DistributedWMS.Application.Stock;
using DistributedWMS.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedWMS.Application;

public static class  DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }
}