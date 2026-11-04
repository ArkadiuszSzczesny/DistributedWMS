using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Infrastructure.Persistence;

/// <summary>
/// Wypełnia bazę danymi początkowymi wraz z przykładową historią obrotu
/// magazynowego. Operacja jest idempotentna — ponowne uruchomienie
/// nie powiela istniejących rekordów.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(WmsDbContext db, CancellationToken ct = default)
    {
        if (await db.Locations.AnyAsync(ct))
            return;

        var locations = SeedLocations(db);
        var users = SeedUsers(db, locations);
        var products = SeedProducts(db);

        await db.SaveChangesAsync(ct);

        await SeedDocumentsAsync(db, locations, users, products, ct);
    }

    // ---------- słowniki ----------

    private static (Location Main, Location Van1, Location Van2) SeedLocations(WmsDbContext db)
    {
        var main = new Location("MG-01", "Magazyn główny", LocationType.MainWarehouse);

        var van1 = new Location("AUTO-01", "Serwis mobilny nr 1", LocationType.MobileWarehouse)
        {
            VehicleRegistration = "EL 12345"
        };

        var van2 = new Location("AUTO-02", "Serwis mobilny nr 2", LocationType.MobileWarehouse)
        {
            VehicleRegistration = "EL 67890"
        };

        db.Locations.AddRange(main, van1, van2);
        return (main, van1, van2);
    }

    private static (User Manager, User Operator, User Tech1, User Tech2) SeedUsers(
        WmsDbContext db, (Location Main, Location Van1, Location Van2) locations)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Haslo123!");

        var manager = new User("kierownik", "Anna Kowalska", hash, UserRole.Manager);
        var warehouseOperator = new User("magazynier", "Piotr Nowak", hash, UserRole.WarehouseOperator);

        var tech1 = new User("serwisant1", "Marek Wiśniewski", hash, UserRole.Technician);
        tech1.AssignLocation(locations.Van1);

        var tech2 = new User("serwisant2", "Tomasz Lewandowski", hash, UserRole.Technician);
        tech2.AssignLocation(locations.Van2);

        db.Users.AddRange(manager, warehouseOperator, tech1, tech2);
        return (manager, warehouseOperator, tech1, tech2);
    }

    private static Dictionary<string, Product> SeedProducts(WmsDbContext db)
    {
        var products = new[]
        {
            new Product("KAB-YDY-3X25", "Przewód YDY 3x2,5 mm2", "m", 200m)
            {
                Description = "Przewód instalacyjny miedziany, izolacja PVC"
            },
            new Product("KAB-YDY-3X15", "Przewód YDY 3x1,5 mm2", "m", 300m),
            new Product("GNI-230-BIA", "Gniazdo pojedyncze 230V białe", "szt", 50m),
            new Product("WYL-POJ-BIA", "Wyłącznik pojedynczy biały", "szt", 50m),
            new Product("BEZ-C16", "Wyłącznik nadprądowy C16", "szt", 30m),
            new Product("BEZ-B10", "Wyłącznik nadprądowy B10", "szt", 30m),
            new Product("PUS-FI60", "Puszka podtynkowa fi 60", "szt", 100m),
            new Product("LED-GU10-5W", "Żarówka LED GU10 5W", "szt", 40m),
            new Product("TAS-IZO-19", "Taśma izolacyjna 19 mm", "szt", 20m),
            new Product("ZLA-WAGO-221", "Złączka WAGO 221-413", "szt", 150m),
            new Product("ROZ-RN-12", "Rozdzielnica natynkowa 12 modułów", "szt", 5m),
            new Product("CZU-RUCH-360", "Czujnik ruchu sufitowy 360°", "szt", 10m)
        };

        db.Products.AddRange(products);
        return products.ToDictionary(p => p.Sku);
    }

    // ---------- historia obrotu ----------

    private static async Task SeedDocumentsAsync(
        WmsDbContext db,
        (Location Main, Location Van1, Location Van2) loc,
        (User Manager, User Operator, User Tech1, User Tech2) users,
        Dictionary<string, Product> products,
        CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;

        // PZ/2026/0001 — dostawa główna
        await PostAsync(db, new Document(
            "PZ/2026/0001", DocumentType.GoodsReceipt, users.Operator.Id,
            null, loc.Main.Id, "FV 2026/10/0145")
        { Notes = "Dostawa od hurtowni elektrotechnicznej" },
        day.AddDays(-14),
        new[]
        {
            (products["KAB-YDY-3X25"].Id, 500m, (decimal?)4.20m),
            (products["KAB-YDY-3X15"].Id, 800m, (decimal?)2.80m),
            (products["GNI-230-BIA"].Id, 120m, (decimal?)8.50m),
            (products["WYL-POJ-BIA"].Id, 100m, (decimal?)7.90m),
            (products["PUS-FI60"].Id, 300m, (decimal?)0.85m),
            (products["ZLA-WAGO-221"].Id, 400m, (decimal?)1.15m)
        }, ct);

        // PZ/2026/0002 — dostawa uzupełniająca
        await PostAsync(db, new Document(
            "PZ/2026/0002", DocumentType.GoodsReceipt, users.Operator.Id,
            null, loc.Main.Id, "FV 2026/10/0188"),
        day.AddDays(-9),
        new[]
        {
            (products["BEZ-C16"].Id, 60m, (decimal?)14.30m),
            (products["LED-GU10-5W"].Id, 80m, (decimal?)11.00m),
            (products["ROZ-RN-12"].Id, 8m, (decimal?)89.00m)
        }, ct);

        // MM/2026/0001 — wyposażenie auta serwisanta 1
        await PostAsync(db, new Document(
            "MM/2026/0001", DocumentType.StockTransfer, users.Operator.Id,
            loc.Main.Id, loc.Van1.Id, null)
        { Notes = "Wyposażenie samochodu na tydzień roboczy" },
        day.AddDays(-7),
        new[]
        {
            (products["KAB-YDY-3X25"].Id, 120m, (decimal?)null),
            (products["GNI-230-BIA"].Id, 30m, (decimal?)null),
            (products["WYL-POJ-BIA"].Id, 25m, (decimal?)null),
            (products["PUS-FI60"].Id, 80m, (decimal?)null),
            (products["ZLA-WAGO-221"].Id, 100m, (decimal?)null)
        }, ct);

        // MM/2026/0002 — wyposażenie auta serwisanta 2
        await PostAsync(db, new Document(
            "MM/2026/0002", DocumentType.StockTransfer, users.Operator.Id,
            loc.Main.Id, loc.Van2.Id, null),
        day.AddDays(-7),
        new[]
        {
            (products["KAB-YDY-3X15"].Id, 200m, (decimal?)null),
            (products["BEZ-C16"].Id, 20m, (decimal?)null),
            (products["LED-GU10-5W"].Id, 30m, (decimal?)null)
        }, ct);

        // RW/2026/0001 — zużycie na zleceniu
        await PostAsync(db, new Document(
            "RW/2026/0001", DocumentType.InternalIssue, users.Tech1.Id,
            loc.Van1.Id, null, "ZL/2026/0088")
        { Notes = "Modernizacja instalacji w lokalu usługowym" },
        day.AddDays(-5),
        new[]
        {
            (products["KAB-YDY-3X25"].Id, 65m, (decimal?)null),
            (products["GNI-230-BIA"].Id, 12m, (decimal?)null),
            (products["PUS-FI60"].Id, 18m, (decimal?)null)
        }, ct);

        // RW/2026/0002 — zużycie serwisanta 2
        await PostAsync(db, new Document(
            "RW/2026/0002", DocumentType.InternalIssue, users.Tech2.Id,
            loc.Van2.Id, null, "ZL/2026/0091"),
        day.AddDays(-3),
        new[]
        {
            (products["KAB-YDY-3X15"].Id, 140m, (decimal?)null),
            (products["BEZ-C16"].Id, 14m, (decimal?)null),
            (products["LED-GU10-5W"].Id, 22m, (decimal?)null)
        }, ct);

        // RW/2026/0003 — zużycie serwisanta 1
        await PostAsync(db, new Document(
            "RW/2026/0003", DocumentType.InternalIssue, users.Tech1.Id,
            loc.Van1.Id, null, "ZL/2026/0094"),
        day.AddDays(-1),
        new[]
        {
            (products["KAB-YDY-3X25"].Id, 40m, (decimal?)null),
            (products["ZLA-WAGO-221"].Id, 60m, (decimal?)null)
        }, ct);

        // RW/2026/0004 — dokument roboczy, nie wpływa na stany
        var draft = new Document(
            "RW/2026/0004", DocumentType.InternalIssue, users.Tech1.Id,
            loc.Van1.Id, null, "ZL/2026/0097")
        { Notes = "Zlecenie w trakcie realizacji" };

        draft.AddLine(products["GNI-230-BIA"].Id, 8m);
        draft.AddLine(products["PUS-FI60"].Id, 15m);

        db.Documents.Add(draft);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Tworzy dokument wraz z pozycjami, zatwierdza go i aktualizuje stany
    /// magazynowe, odwzorowując przebieg rzeczywistej operacji w systemie.
    /// </summary>
    private static async Task PostAsync(
        WmsDbContext db,
        Document document,
        DateTime documentDate,
        (Guid ProductId, decimal Quantity, decimal? UnitPrice)[] lines,
        CancellationToken ct)
    {
        foreach (var line in lines)
            document.AddLine(line.ProductId, line.Quantity, line.UnitPrice);

        foreach (var line in lines)
        {
            if (document.SourceLocationId is not null)
                Decrease(db, line.ProductId, document.SourceLocationId.Value, line.Quantity);

            if (document.TargetLocationId is not null)
                Increase(db, line.ProductId, document.TargetLocationId.Value, line.Quantity);
        }

        document.MarkAsPosted();
        db.Documents.Add(document);

        await db.SaveChangesAsync(ct);

        // Daty dokumentów rozkładane są wstecz, aby lista prezentowała
        // historię obrotu zamiast rekordów o identycznym znaczniku czasu.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Documents\" SET \"DocumentDate\" = {0}, \"PostedAtUtc\" = {0} WHERE \"Id\" = {1}",
            new object[] { documentDate, document.Id }, ct);
    }

    private static void Increase(WmsDbContext db, Guid productId, Guid locationId, decimal quantity)
    {
        var stock = db.StockItems.Local
            .FirstOrDefault(s => s.ProductId == productId && s.LocationId == locationId)
            ?? db.StockItems.FirstOrDefault(s => s.ProductId == productId && s.LocationId == locationId);

        if (stock is null)
        {
            stock = new StockItem(productId, locationId);
            db.StockItems.Add(stock);
        }

        stock.Increase(quantity);
    }

    private static void Decrease(WmsDbContext db, Guid productId, Guid locationId, decimal quantity)
    {
        var stock = db.StockItems.Local
            .FirstOrDefault(s => s.ProductId == productId && s.LocationId == locationId)
            ?? db.StockItems.FirstOrDefault(s => s.ProductId == productId && s.LocationId == locationId);

        if (stock is null)
        {
            var sku = db.Products.First(p => p.Id == productId).Sku;
            var code = db.Locations.First(l => l.Id == locationId).Code;

            throw new InvalidOperationException(
                $"Dane początkowe: towar {sku} nie występuje w lokalizacji {code}. " +
                "Sprawdź kolejność dokumentów w seederze.");
        }

        stock.Decrease(quantity);
    }
}