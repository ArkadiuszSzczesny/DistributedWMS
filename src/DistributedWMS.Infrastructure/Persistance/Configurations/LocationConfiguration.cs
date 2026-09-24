using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedWMS.Infrastructure.Persistance.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.VehicleRegistration).HasMaxLength(15);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Ignore(x => x.IsMobile);
    }
}
