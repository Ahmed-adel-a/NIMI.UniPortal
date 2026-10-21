using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "materials");

            migrationBuilder.CreateTable(
                name: "materials",
                schema: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    week_number = table.Column<int>(type: "integer", nullable: true),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    download_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                    table.CheckConstraint("ck_materials_download_count", "download_count >= 0");
                    table.CheckConstraint("ck_materials_file_type", "file_type IN ('pdf','ppt','doc','xls','zip','image','video','other')");
                    table.CheckConstraint("ck_materials_size_bytes", "size_bytes > 0");
                    table.CheckConstraint("ck_materials_week_number", "week_number IS NULL OR week_number BETWEEN 1 AND 30");
                });

            migrationBuilder.CreateIndex(
                name: "ix_materials_subject_id_term_id_is_deleted",
                schema: "materials",
                table: "materials",
                columns: new[] { "subject_id", "term_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "materials",
                schema: "materials");
        }
    }
}
