using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Infrastructure.Persistance;

/// <summary>
/// Wypełnia bazę danymi początkowymi na potrzeby uruchomienia
/// i weryfikacji działania systemu. Operacja jest idempotentna -
/// ponowne uruchomienie nie powiela stniejących rekordów.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(WmsDbContext db, CancellationToken ct = default)
    {
        if (await db.Locations.AnyAsync(ct))
            return;
    

        // --- lokalizacje ---
        var mainWarehouse = new Location("MG-01", "Magazyn główny", LocationType.MainWarehouse);

        var van1 = new Location("AUTO-01", "Serwis mobilny nr 1", LocationType.MobileWarehouse)
        {
            VehicleRegistration = "El 12345"
        };

        var van2 = new Location("AUTO-02", "Serwis mobilny nr 2", LocationType.MobileWarehouse)
        {
            VehicleRegistration = "El 67890"
        };

        db.Locations.AddRange(mainWarehouse, van1, van2);

        // --- użytkownicy ---
        // Hasła demonstarcyjne; w środowisku produkcyjnym wymoszona byłaby ich zmiana.
        var hash = BCrypt.Net.BCrypt.HashPassword("Haslo123!");

        var manager = new User("kierownik", "Anna Kowalska", hash, UserRole.Manager);
        var operatorUser = new User("magazynier", "Piotr Nowak", hash, UserRole.WarehouseOperator);

        var tech1 = new User("serwisant1", "Marek Wiśniewski", hash, UserRole.Technician);
        tech1.AssignLocation(van1);

        var tech2 = new User("serwisant2", "Tomasz Lewandowski", hash, UserRole.Technician);
        tech2.AssignLocation(van2);

        db.Users.AddRange(manager, operatorUser, tech1, tech2);

        // --- kartoteka towarowa ---
        var products = new[]
        {
            new Product("KAB-YDY-3X25", "Przewód YDY 3x2,5 mm2", "m", 200m)
            {
                Description = "Przewód instalacyjny miedziany"
            },
            new Product("GNI-230-BIA", "Gniazdo pojednycze 230V białe", "szt", 50m),
            new Product("WYL-POJ-BIA", "Wyłącznik pojedynczy biały", "szt", 50m),
            new Product("BEZ-C16", "Wyłącznik naprądowy C16", "szt", 30m),
            new Product("PUS-FI60", "Puszka podtynkowa fi 60", "szt", 100m),
            new Product("LED-GU10-5W", "Żarówka LED GU10 5W", "szt", 40),
            new Product("TAS-IZO-19", "Taśma izolacyjna 19 mm", "szt", 20m),
            new Product("ZLA-WAGO-221", "Złączka WAGO 221-413", "szt", 150m)
        };

        db.Products.AddRange(products);

        await db.SaveChangesAsync(ct);
    }
}