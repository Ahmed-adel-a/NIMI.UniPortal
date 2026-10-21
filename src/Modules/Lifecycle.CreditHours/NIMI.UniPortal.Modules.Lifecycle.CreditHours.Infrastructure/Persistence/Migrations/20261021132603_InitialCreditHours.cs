using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreditHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lifecycle_credit");

            migrationBuilder.CreateTable(
                name: "course_registrations",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_hours = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dropped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_registrations", x => x.id);
                    table.CheckConstraint("ck_course_registrations_status", "status IN ('registered','pending_payment','dropped','completed','failed')");
                });

            migrationBuilder.CreateTable(
                name: "credit_ledger",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_hours = table.Column<int>(type: "integer", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    grade_points = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_ledger", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_seats",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    taken = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_seats", x => x.subject_group_id);
                    table.CheckConstraint("ck_group_seats_capacity", "capacity >= 0");
                    table.CheckConstraint("ck_group_seats_taken", "taken >= 0 AND taken <= capacity");
                });

            migrationBuilder.CreateTable(
                name: "registration_windows",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    min_credits = table.Column<int>(type: "integer", nullable: false),
                    max_credits = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_windows", x => x.id);
                    table.CheckConstraint("ck_registration_windows_credits", "min_credits >= 0 AND min_credits <= max_credits AND max_credits <= 40");
                    table.CheckConstraint("ck_registration_windows_dates", "closes_at > opens_at");
                });

            migrationBuilder.CreateTable(
                name: "subject_prerequisites",
                schema: "lifecycle_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prerequisite_subject_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_prerequisites", x => x.id);
                    table.CheckConstraint("ck_subject_prerequisites_not_self", "subject_id <> prerequisite_subject_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_course_registrations_student_id_term_id_subject_id",
                schema: "lifecycle_credit",
                table: "course_registrations",
                columns: new[] { "student_id", "term_id", "subject_id" },
                unique: true,
                filter: "status <> 'dropped'");

            migrationBuilder.CreateIndex(
                name: "ix_course_registrations_subject_group_id_status",
                schema: "lifecycle_credit",
                table: "course_registrations",
                columns: new[] { "subject_group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_ledger_student_id_term_id_subject_id",
                schema: "lifecycle_credit",
                table: "credit_ledger",
                columns: new[] { "student_id", "term_id", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_windows_term_id",
                schema: "lifecycle_credit",
                table: "registration_windows",
                column: "term_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subject_prerequisites_subject_id_prerequisite_subject_id",
                schema: "lifecycle_credit",
                table: "subject_prerequisites",
                columns: new[] { "subject_id", "prerequisite_subject_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_registrations",
                schema: "lifecycle_credit");

            migrationBuilder.DropTable(
                name: "credit_ledger",
                schema: "lifecycle_credit");

            migrationBuilder.DropTable(
                name: "group_seats",
                schema: "lifecycle_credit");

            migrationBuilder.DropTable(
                name: "registration_windows",
                schema: "lifecycle_credit");

            migrationBuilder.DropTable(
                name: "subject_prerequisites",
                schema: "lifecycle_credit");
        }
    }
}
