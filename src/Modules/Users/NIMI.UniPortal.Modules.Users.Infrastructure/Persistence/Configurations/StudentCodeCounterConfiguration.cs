using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Configurations;

internal sealed class StudentCodeCounterConfiguration : IEntityTypeConfiguration<StudentCodeCounter>
{
    public void Configure(EntityTypeBuilder<StudentCodeCounter> builder)
    {
        builder.ToTable("student_code_counters", table => table.HasCheckConstraint("ck_student_code_counters_last_value", "last_value >= 1"));
        builder.HasKey(c => c.CounterKey);
        builder.Property(c => c.CounterKey).HasMaxLength(100);
    }
}
