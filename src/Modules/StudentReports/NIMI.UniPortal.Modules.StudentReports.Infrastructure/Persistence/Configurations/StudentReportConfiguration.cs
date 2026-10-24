using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence.Configurations;

/// <summary>student_reports.reports — one row per report; the student and department are other modules' ids.</summary>
internal sealed class StudentReportConfiguration : IEntityTypeConfiguration<StudentReport>
{
    public void Configure(EntityTypeBuilder<StudentReport> builder)
    {
        builder.ToTable("reports", table =>
        {
            table.HasCheckConstraint("ck_reports_category", "category IN ('academic','attendance','conduct','other')");
            table.HasCheckConstraint("ck_reports_status", "status IN ('open','answered','closed')");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Title).HasMaxLength(StudentReport.MaxTitleLength);
        builder.Property(r => r.Message).HasMaxLength(StudentReport.MaxTextLength);
        builder.Property(r => r.Reply).HasMaxLength(StudentReport.MaxTextLength);
        builder.Property(r => r.Category)
            .HasConversion(c => Name(c.ToString()), text => Enum.Parse<ReportCategory>(text, true))
            .HasMaxLength(20);
        builder.Property(r => r.Status)
            .HasConversion(s => Name(s.ToString()), text => Enum.Parse<ReportStatus>(text, true))
            .HasMaxLength(20);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasIndex(r => new { r.DepartmentId, r.Status, r.CreatedAt });
        builder.HasIndex(r => new { r.StudentId, r.CreatedAt });
    }

    private static string Name(string value) => JsonNamingPolicy.SnakeCaseLower.ConvertName(value);
}
