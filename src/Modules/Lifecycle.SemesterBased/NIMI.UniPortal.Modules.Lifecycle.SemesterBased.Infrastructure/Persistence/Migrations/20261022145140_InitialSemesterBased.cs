using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSemesterBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lifecycle_semester");

            migrationBuilder.CreateTable(
                name: "promotion_policies",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_carried_subjects = table.Column<int>(type: "integer", nullable: false),
                    gate_levels = table.Column<int[]>(type: "integer[]", nullable: false),
                    max_years_per_level = table.Column<int>(type: "integer", nullable: false),
                    require_fees_paid = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_policies", x => x.id);
                    table.CheckConstraint("ck_promotion_policies_max_carried", "max_carried_subjects BETWEEN 0 AND 10");
                    table.CheckConstraint("ck_promotion_policies_max_years", "max_years_per_level BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "student_standings",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    years_in_level = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_standings", x => x.id);
                    table.CheckConstraint("ck_student_standings_level", "level BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_student_standings_status", "status IN ('active','repeating','external','on_hold','graduated','withdrawn')");
                });

            migrationBuilder.CreateTable(
                name: "study_plans",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    levels = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_plans", x => x.id);
                    table.CheckConstraint("ck_study_plans_levels", "levels BETWEEN 1 AND 7");
                });

            migrationBuilder.CreateTable(
                name: "term_enrollments",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    credit_hours = table.Column<int>(type: "integer", nullable: false),
                    is_carried = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grade_points = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    result_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_term_enrollments", x => x.id);
                    table.CheckConstraint("ck_term_enrollments_status", "status IN ('enrolled','passed','failed','withdrawn')");
                });

            migrationBuilder.CreateTable(
                name: "year_promotions",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    from_level = table.Column<int>(type: "integer", nullable: false),
                    to_level = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    failed_subjects = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_year_promotions", x => x.id);
                    table.CheckConstraint("ck_year_promotions_result", "result IN ('promoted','promoted_with_carried','repeat','external','graduated','held')");
                });

            migrationBuilder.CreateTable(
                name: "study_plan_subjects",
                schema: "lifecycle_semester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_plan_subjects", x => x.id);
                    table.CheckConstraint("ck_study_plan_subjects_term", "term IN ('first','second')");
                    table.ForeignKey(
                        name: "fk_study_plan_subjects_study_plans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalSchema: "lifecycle_semester",
                        principalTable: "study_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_policies_faculty_id",
                schema: "lifecycle_semester",
                table: "promotion_policies",
                column: "faculty_id",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_student_standings_department_id_level_status",
                schema: "lifecycle_semester",
                table: "student_standings",
                columns: new[] { "department_id", "level", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_student_standings_student_id",
                schema: "lifecycle_semester",
                table: "student_standings",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_plan_subjects_study_plan_id_subject_id",
                schema: "lifecycle_semester",
                table: "study_plan_subjects",
                columns: new[] { "study_plan_id", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_plans_department_id",
                schema: "lifecycle_semester",
                table: "study_plans",
                column: "department_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_term_enrollments_student_id_term_id_subject_id",
                schema: "lifecycle_semester",
                table: "term_enrollments",
                columns: new[] { "student_id", "term_id", "subject_id" },
                unique: true,
                filter: "status <> 'withdrawn'");

            migrationBuilder.CreateIndex(
                name: "ix_term_enrollments_subject_group_id_status",
                schema: "lifecycle_semester",
                table: "term_enrollments",
                columns: new[] { "subject_group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_year_promotions_academic_year_department_id",
                schema: "lifecycle_semester",
                table: "year_promotions",
                columns: new[] { "academic_year", "department_id" });

            migrationBuilder.CreateIndex(
                name: "ix_year_promotions_student_id_academic_year",
                schema: "lifecycle_semester",
                table: "year_promotions",
                columns: new[] { "student_id", "academic_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_policies",
                schema: "lifecycle_semester");

            migrationBuilder.DropTable(
                name: "student_standings",
                schema: "lifecycle_semester");

            migrationBuilder.DropTable(
                name: "study_plan_subjects",
                schema: "lifecycle_semester");

            migrationBuilder.DropTable(
                name: "term_enrollments",
                schema: "lifecycle_semester");

            migrationBuilder.DropTable(
                name: "year_promotions",
                schema: "lifecycle_semester");

            migrationBuilder.DropTable(
                name: "study_plans",
                schema: "lifecycle_semester");
        }
    }
}
