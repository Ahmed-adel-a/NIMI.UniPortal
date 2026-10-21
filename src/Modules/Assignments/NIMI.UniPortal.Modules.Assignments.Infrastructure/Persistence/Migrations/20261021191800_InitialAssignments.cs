using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assignments");

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    instructions = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    max_grade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    allowed_extensions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    max_file_mb = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.CheckConstraint("ck_assignments_max_file_mb", "max_file_mb BETWEEN 1 AND 50");
                    table.CheckConstraint("ck_assignments_max_grade", "max_grade > 0");
                });

            migrationBuilder.CreateTable(
                name: "receipt_counters",
                schema: "assignments",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_counters", x => x.year);
                    table.CheckConstraint("ck_receipt_counters_last_value", "last_value >= 0");
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                schema: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    student_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    feedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.id);
                    table.CheckConstraint("ck_submissions_size_bytes", "size_bytes > 0");
                    table.CheckConstraint("ck_submissions_status", "status IN ('submitted','approved','rejected')");
                    table.ForeignKey(
                        name: "fk_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalSchema: "assignments",
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_subject_id_term_id_is_deleted",
                schema: "assignments",
                table: "assignments",
                columns: new[] { "subject_id", "term_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_submissions_assignment_id_student_id",
                schema: "assignments",
                table: "submissions",
                columns: new[] { "assignment_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_receipt_number",
                schema: "assignments",
                table: "submissions",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_student_id_submitted_at",
                schema: "assignments",
                table: "submissions",
                columns: new[] { "student_id", "submitted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt_counters",
                schema: "assignments");

            migrationBuilder.DropTable(
                name: "submissions",
                schema: "assignments");

            migrationBuilder.DropTable(
                name: "assignments",
                schema: "assignments");
        }
    }
}
