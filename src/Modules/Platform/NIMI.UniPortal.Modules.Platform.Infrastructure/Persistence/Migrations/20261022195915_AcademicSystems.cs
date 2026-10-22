using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AcademicSystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_system_rules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    system = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_system_rules", x => x.id);
                    table.CheckConstraint("ck_academic_system_rules_scope", "faculty_id IS NULL OR department_id IS NULL");
                    table.CheckConstraint("ck_academic_system_rules_system", "system IN ('credit_hours','year_groups')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_system_rules_faculty_id_department_id",
                schema: "platform",
                table: "academic_system_rules",
                columns: new[] { "faculty_id", "department_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "academic_system_rules",
                schema: "platform");
        }
    }
}
