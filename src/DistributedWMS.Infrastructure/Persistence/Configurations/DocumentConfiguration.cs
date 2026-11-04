using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedWMS.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", t=>
        {
            t.HasCheckConstraint(
                "CK_Documents_DifferentLocations",
                "\"SourceLocationId\" IS NULL OR \"TargetLocationId\" IS NULL OR \"SourceLocationId\" <> \"TargetLocationId\"");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.DocumentDate);
        builder.HasIndex(x => new { x.Type, x.Status });

        builder.HasOne(x => x.SourceLocation)
            .WithMany()
            .HasForeignKey(x => x.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetLocation)
           .WithMany()
           .HasForeignKey(x => x.TargetLocationId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
           .WithMany()
           .HasForeignKey(x => x.CreatedByUserId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
           .WithOne(l => l.Document)
           .HasForeignKey(l => l.DocumentId)
           .OnDelete(DeleteBehavior.Cascade);

        // EF Core zapisuje pozycje przez prywatne pole _lines, bo publiczna kolekcja jest tylko do odczytu.
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.IsEditable);
    }
}
