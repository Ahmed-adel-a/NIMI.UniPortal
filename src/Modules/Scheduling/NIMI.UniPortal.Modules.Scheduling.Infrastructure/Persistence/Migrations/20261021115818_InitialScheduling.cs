using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "facilities",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    floor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facilities", x => x.id);
                    table.CheckConstraint("ck_facilities_capacity", "capacity BETWEEN 1 AND 5000");
                    table.CheckConstraint("ck_facilities_type", "type IN ('lecture_hall','lab','auditorium','classroom','workshop')");
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    credit_hours = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_graduation_requirement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subjects", x => x.id);
                    table.CheckConstraint("ck_subjects_category", "category IN ('regular','military_education')");
                    table.CheckConstraint("ck_subjects_credit_hours", "credit_hours BETWEEN 0 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "terms",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    week_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms", x => x.id);
                    table.CheckConstraint("ck_terms_dates", "ends_on > starts_on");
                    table.CheckConstraint("ck_terms_number", "number IN ('first','second','summer')");
                    table.CheckConstraint("ck_terms_status", "status IN ('planned','active','closed')");
                    table.CheckConstraint("ck_terms_week_count", "week_count BETWEEN 1 AND 30");
                });

            migrationBuilder.CreateTable(
                name: "subject_groups",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_number = table.Column<int>(type: "integer", nullable: false),
                    target_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_groups", x => x.id);
                    table.CheckConstraint("ck_subject_groups_capacity", "capacity BETWEEN 1 AND 1000");
                    table.CheckConstraint("ck_subject_groups_number", "group_number >= 1");
                    table.ForeignKey(
                        name: "fk_subject_groups_subjects_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "scheduling",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_groups_terms_term_id",
                        column: x => x.term_id,
                        principalSchema: "scheduling",
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schedules",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    recurrence = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedules", x => x.id);
                    table.CheckConstraint("ck_schedules_day", "day_of_week BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_schedules_recurrence", "recurrence IN ('weekly','biweekly','once')");
                    table.CheckConstraint("ck_schedules_time", "end_time > start_time");
                    table.ForeignKey(
                        name: "fk_schedules_facilities_facility_id",
                        column: x => x.facility_id,
                        principalSchema: "scheduling",
                        principalTable: "facilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_schedules_subject_groups_subject_group_id",
                        column: x => x.subject_group_id,
                        principalSchema: "scheduling",
                        principalTable: "subject_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teaching_assignments",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teaching_assignments", x => x.id);
                    table.CheckConstraint("ck_teaching_assignments_role", "role IN ('lecturer','lab','tutorial')");
                    table.ForeignKey(
                        name: "fk_teaching_assignments_subject_groups_subject_group_id",
                        column: x => x.subject_group_id,
                        principalSchema: "scheduling",
                        principalTable: "subject_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_teaching_assignments_subjects_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "scheduling",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_teaching_assignments_terms_term_id",
                        column: x => x.term_id,
                        principalSchema: "scheduling",
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_facilities_faculty_id",
                schema: "scheduling",
                table: "facilities",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_facilities_faculty_id_name",
                schema: "scheduling",
                table: "facilities",
                columns: new[] { "faculty_id", "name" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_schedules_facility_id_day_of_week",
                schema: "scheduling",
                table: "schedules",
                columns: new[] { "facility_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_schedules_staff_id_day_of_week",
                schema: "scheduling",
                table: "schedules",
                columns: new[] { "staff_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_schedules_subject_group_id",
                schema: "scheduling",
                table: "schedules",
                column: "subject_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_groups_subject_id_term_id_group_number",
                schema: "scheduling",
                table: "subject_groups",
                columns: new[] { "subject_id", "term_id", "group_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subject_groups_target_department_id",
                schema: "scheduling",
                table: "subject_groups",
                column: "target_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_groups_term_id",
                schema: "scheduling",
                table: "subject_groups",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_subjects_code",
                schema: "scheduling",
                table: "subjects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_department_id",
                schema: "scheduling",
                table: "subjects",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_staff_id",
                schema: "scheduling",
                table: "teaching_assignments",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_staff_id_subject_id_term_id_subject_gr",
                schema: "scheduling",
                table: "teaching_assignments",
                columns: new[] { "staff_id", "subject_id", "term_id", "subject_group_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_subject_group_id",
                schema: "scheduling",
                table: "teaching_assignments",
                column: "subject_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_subject_id",
                schema: "scheduling",
                table: "teaching_assignments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_term_id",
                schema: "scheduling",
                table: "teaching_assignments",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_terms_academic_year_number",
                schema: "scheduling",
                table: "terms",
                columns: new[] { "academic_year", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terms_status",
                schema: "scheduling",
                table: "terms",
                column: "status",
                unique: true,
                filter: "status = 'active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedules",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "teaching_assignments",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "facilities",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "subject_groups",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "subjects",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "terms",
                schema: "scheduling");
        }
    }
}
