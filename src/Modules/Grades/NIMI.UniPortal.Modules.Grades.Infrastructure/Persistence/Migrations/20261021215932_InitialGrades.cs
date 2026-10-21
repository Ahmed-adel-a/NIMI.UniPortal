using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "grades");

            migrationBuilder.CreateTable(
                name: "final_grades",
                schema: "grades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_hours = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    grade_points = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    drafted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_final_grades", x => x.id);
                    table.CheckConstraint("ck_final_grades_credit_hours", "credit_hours >= 0");
                    table.CheckConstraint("ck_final_grades_score", "score >= 0 AND score <= 100");
                    table.CheckConstraint("ck_final_grades_status", "status IN ('draft','confirmed','published')");
                });

            migrationBuilder.CreateTable(
                name: "grade_corrections",
                schema: "grades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    final_grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    new_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    old_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    new_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    corrected_by = table.Column<Guid>(type: "uuid", nullable: false),
                    corrected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grade_corrections", x => x.id);
                    table.ForeignKey(
                        name: "fk_grade_corrections_final_grades_final_grade_id",
                        column: x => x.final_grade_id,
                        principalSchema: "grades",
                        principalTable: "final_grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_final_grades_student_id_subject_id_term_id",
                schema: "grades",
                table: "final_grades",
                columns: new[] { "student_id", "subject_id", "term_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_final_grades_subject_group_id",
                schema: "grades",
                table: "final_grades",
                column: "subject_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_final_grades_term_id_status",
                schema: "grades",
                table: "final_grades",
                columns: new[] { "term_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_grade_corrections_final_grade_id",
                schema: "grades",
                table: "grade_corrections",
                column: "final_grade_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grade_corrections",
                schema: "grades");

            migrationBuilder.DropTable(
                name: "final_grades",
                schema: "grades");
        }
    }
}
