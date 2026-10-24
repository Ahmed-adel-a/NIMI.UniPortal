using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialStudentReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "student_reports");

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "student_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reply = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    answered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.CheckConstraint("ck_reports_category", "category IN ('academic','attendance','conduct','other')");
                    table.CheckConstraint("ck_reports_status", "status IN ('open','answered','closed')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_department_id_status_created_at",
                schema: "student_reports",
                table: "reports",
                columns: new[] { "department_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_student_id_created_at",
                schema: "student_reports",
                table: "reports",
                columns: new[] { "student_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "student_reports");
        }
    }
}
