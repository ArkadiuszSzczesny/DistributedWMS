using DistributedWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DistributedWMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.UserName).IsUnique();

        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Relacja 1:1 - jeden samochód może być przypisany tylko jednemu serwisantowi.
        builder.HasOne(x => x.AssignedLocation)
            .WithOne()
            .HasForeignKey<User>(x => x.AssignedLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

 
