using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Infrastructure.Persistance;

///<summary>
///Kontekst bazy danych systemu. Konfiguracje poszczególnych encji
///znajdują się w osobnych klasach w folderze Configurations.
/// </summary>
public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentLine> DocumentLines => Set<DocumentLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
    }
}