using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GraduationAndGpa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gpa_snapshots",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    term_gpa = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    cumulative_gpa = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    credits_attempted = table.Column<int>(type: "integer", nullable: false),
                    credits_earned = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gpa_snapshots", x => x.id);
                    table.CheckConstraint("ck_gpa_snapshots_credits", "credits_attempted >= 0 AND credits_earned >= 0");
                });

            migrationBuilder.CreateTable(
                name: "graduation_requirements",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_credits = table.Column<int>(type: "integer", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_graduation_requirements", x => x.department_id);
                    table.CheckConstraint("ck_graduation_requirements_credits", "required_credits > 0 AND required_credits <= 400");
                });

            migrationBuilder.CreateIndex(
                name: "ix_gpa_snapshots_student_id_term_id",
                schema: "lifecycle_credit",
                table: "gpa_snapshots",
                columns: new[] { "student_id", "term_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gpa_snapshots",
                schema: "lifecycle_credit");

            migrationBuilder.DropTable(
                name: "graduation_requirements",
                schema: "lifecycle_credit");
        }
    }
}
