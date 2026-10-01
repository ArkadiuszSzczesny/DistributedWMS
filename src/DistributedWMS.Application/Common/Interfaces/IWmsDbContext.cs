using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DistributedWMS.Application.Common.Interfaces;

/// <summary>
/// Kontrakt dostępu do danych wykorzystywany przez warstwę aplikacji.
/// Umożliwia odseparowanie logiki biznesowej od konkretnej implementacji
/// kontekstu bazy danych oraz podstawienie atrapy w testach jednostkowych.
/// </summary>
public interface IWmsDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Location> Locations { get; }
    DbSet<User> Users { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentLine> DocumentLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Rozpoczyna jawną transakcję bazodanową obejmującą wiele operacji zapisu. </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

