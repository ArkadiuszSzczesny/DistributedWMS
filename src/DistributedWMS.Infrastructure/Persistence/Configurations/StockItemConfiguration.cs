using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DistributedWMS.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", t =>
        {
            // Druga linia obrony nawet przy błedzie w aplikacji baza odrzuci stan ujemny.
            t.HasCheckConstraint("CK_StockItems_Quantity_NonNegative", "\"Quantity\" >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Quantity).HasPrecision(18, 3);

        // Jedna para towar-lokalizacja może mieć tylko jeden rekord stanu.
        builder.HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();

        // Blokada optymistyczna - Npgsql mapuje właściwość uint na systemową kolumnę xmin.
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Product)
             .WithMany(p => p.StockItems)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(l => l.StockItems)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


