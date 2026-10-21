using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

internal sealed class GroupSeatConfiguration : IEntityTypeConfiguration<GroupSeat>
{
    public void Configure(EntityTypeBuilder<GroupSeat> builder)
    {
        builder.ToTable("group_seats", table =>
        {
            table.HasCheckConstraint("ck_group_seats_capacity", "capacity >= 0");
            table.HasCheckConstraint("ck_group_seats_taken", "taken >= 0 AND taken <= capacity");
        });

        builder.HasKey(s => s.SubjectGroupId);
        builder.Property(s => s.SubjectGroupId).ValueGeneratedNever();

        builder.Property(s => s.Capacity);
        builder.Property(s => s.Taken);
    }
}
