using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_role", "role IN ('student', 'professor', 'department_head', 'admin')");
            table.HasCheckConstraint("ck_users_academic_title", "academic_title IN ('professor', 'lecturer', 'demonstrator')");
            table.HasCheckConstraint("ck_users_gender", "gender IN ('male', 'female')");
            table.HasCheckConstraint("ck_users_failed_login_count", "failed_login_count >= 0");
        });

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.FullName).HasMaxLength(255);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.PasswordHash).HasMaxLength(500);
        builder.Property(u => u.Role).HasConversion(role => UserRoles.Name(role), text => UserRoles.Parse(text)).HasMaxLength(20);
        builder.Property(u => u.AcademicTitle).HasConversion(new LowerCaseEnumConverter<AcademicTitle>()).HasMaxLength(20);
        builder.Property(u => u.Gender).HasConversion(new LowerCaseEnumConverter<Gender>()).HasMaxLength(10);
        builder.Property(u => u.StudentCode).HasMaxLength(50);
        builder.Property(u => u.NationalId).HasMaxLength(50);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

        // Same module → a real foreign key. Restrict: departments are deactivated, never deleted.
        builder.HasOne<Department>().WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.Email).IsUnique();         // stored normalized (lower-case) → exact, case-insensitive uniqueness
        builder.HasIndex(u => u.StudentCode).IsUnique();   // NULLs (non-students) don't collide in PostgreSQL
        builder.HasIndex(u => u.Role);

        // Decision ROLES2: at most one active head per department (the handlers check first for a friendly 409).
        // Named, so it doesn't replace the plain department index EF keeps for the foreign key.
        builder.HasIndex(u => u.DepartmentId, "ix_users_department_id");
        builder.HasIndex(u => u.DepartmentId, "ux_users_active_department_head")
            .IsUnique()
            .HasFilter("role = 'department_head' AND is_active")
            .HasDatabaseName("ux_users_active_department_head");
    }
}
