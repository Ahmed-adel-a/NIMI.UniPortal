using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "system_settings",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    university_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    university_name_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    secondary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    week_start_day = table.Column<int>(type: "integer", nullable: false),
                    working_days = table.Column<int[]>(type: "integer[]", nullable: false),
                    attendance_threshold = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    late_grace_minutes = table.Column<int>(type: "integer", nullable: false),
                    grading_scale = table.Column<string>(type: "jsonb", nullable: false),
                    student_code_format = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    military_education_policy = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                    table.CheckConstraint("ck_system_settings_attendance_threshold", "attendance_threshold BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_system_settings_late_grace_minutes", "late_grace_minutes BETWEEN 0 AND 180");
                    table.CheckConstraint("ck_system_settings_singleton", "id = '0199a000-0000-7000-8000-0000000000f1'");
                    table.CheckConstraint("ck_system_settings_week_start_day", "week_start_day BETWEEN 0 AND 6");
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "system_settings",
                columns: new[] { "id", "attendance_threshold", "created_at", "domain", "grading_scale", "late_grace_minutes", "logo_url", "military_education_policy", "primary_color", "secondary_color", "student_code_format", "timezone", "university_name", "university_name_ar", "updated_at", "week_start_day", "working_days" },
                values: new object[] { new Guid("0199a000-0000-7000-8000-0000000000f1"), 75m, new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "[{\"grade\":\"A\\u002B\",\"minScore\":97,\"points\":4.0},{\"grade\":\"A\",\"minScore\":93,\"points\":4.0},{\"grade\":\"A-\",\"minScore\":89,\"points\":3.7},{\"grade\":\"B\\u002B\",\"minScore\":84,\"points\":3.3},{\"grade\":\"B\",\"minScore\":80,\"points\":3.0},{\"grade\":\"B-\",\"minScore\":76,\"points\":2.7},{\"grade\":\"C\\u002B\",\"minScore\":73,\"points\":2.3},{\"grade\":\"C\",\"minScore\":70,\"points\":2.0},{\"grade\":\"C-\",\"minScore\":67,\"points\":1.7},{\"grade\":\"D\\u002B\",\"minScore\":64,\"points\":1.3},{\"grade\":\"D\",\"minScore\":60,\"points\":1.0},{\"grade\":\"F\",\"minScore\":0,\"points\":0.0}]", 10, null, "{\"mode\":\"opt_in\"}", "#0A4CB3", "#5B9BFF", "{DEPT}-{YEAR}-{SEQ:0000}", "Africa/Cairo", "University", "الجامعة", null, 0, new[] { 0, 1, 2, 3, 4, 5 } });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_settings",
                schema: "platform");
        }
    }
}
