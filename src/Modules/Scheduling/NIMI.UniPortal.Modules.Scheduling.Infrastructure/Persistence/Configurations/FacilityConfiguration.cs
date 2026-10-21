using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("facilities", table =>
        {
            table.HasCheckConstraint(
                "ck_facilities_type", "type IN ('lecture_hall','lab','auditorium','classroom','workshop')");
            table.HasCheckConstraint("ck_facilities_capacity", "capacity BETWEEN 1 AND 5000");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.FacultyId);
        builder.Property(f => f.Name).HasMaxLength(100);
        builder.Property(f => f.Type)
            .HasConversion(new SnakeCaseEnumConverter<FacilityType>())
            .HasMaxLength(20);
        builder.Property(f => f.Capacity);
        builder.Property(f => f.Building).HasMaxLength(100);
        builder.Property(f => f.Floor).HasMaxLength(20);
        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedAt);

        // A name is unique within a faculty; shared facilities (null faculty) form one scope.
        builder.HasIndex(f => new { f.FacultyId, f.Name }).IsUnique().AreNullsDistinct(false);
        builder.HasIndex(f => f.FacultyId);
    }
}
