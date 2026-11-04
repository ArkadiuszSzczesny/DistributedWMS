using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DistributedWMS.Infrastructure.Persistence.Configurations;

public class DocumentLineConfiguration : IEntityTypeConfiguration<DocumentLine>
{
    public void Configure(EntityTypeBuilder<DocumentLine> builder)
    {
        builder.ToTable("DocumentLines", t =>
        {
            t.HasCheckConstraint("CK_DocumentLines_Quantity_Positive", "\"Quantity\" > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);

        // Metoda AddLine scala duplikaty, a indeks gwarantuje to samo na poziomie bazy.
        builder.HasIndex(x => new { x.DocumentId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.LineValue);

    }
}