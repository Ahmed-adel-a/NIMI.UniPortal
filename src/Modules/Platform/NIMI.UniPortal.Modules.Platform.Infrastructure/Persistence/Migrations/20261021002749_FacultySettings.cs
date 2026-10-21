using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FacultySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "faculty_settings",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_day = table.Column<int>(type: "integer", nullable: true),
                    working_days = table.Column<int[]>(type: "integer[]", nullable: true),
                    attendance_threshold = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    late_grace_minutes = table.Column<int>(type: "integer", nullable: true),
                    grading_scale = table.Column<string>(type: "jsonb", nullable: true),
                    student_code_format = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    military_education_policy = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faculty_settings", x => x.id);
                    table.CheckConstraint("ck_faculty_settings_attendance_threshold", "attendance_threshold BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_faculty_settings_late_grace_minutes", "late_grace_minutes BETWEEN 0 AND 180");
                    table.CheckConstraint("ck_faculty_settings_week_start_day", "week_start_day BETWEEN 0 AND 6");
                });

            migrationBuilder.CreateIndex(
                name: "ix_faculty_settings_faculty_id",
                schema: "platform",
                table: "faculty_settings",
                column: "faculty_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faculty_settings",
                schema: "platform");
        }
    }
}
