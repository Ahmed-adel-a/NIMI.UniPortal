using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "attendance_sessions",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    opened_by = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    qr_secret = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_sessions", x => x.id);
                    table.CheckConstraint("ck_attendance_sessions_qr_secret", "octet_length(qr_secret) = 32");
                });

            migrationBuilder.CreateTable(
                name: "attendance_warnings",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_warnings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_change_requests",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_change_requests", x => x.id);
                    table.CheckConstraint("ck_device_change_requests_status", "status IN ('pending','approved','rejected')");
                });

            migrationBuilder.CreateTable(
                name: "registered_devices",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registered_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    device_fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                    table.CheckConstraint("ck_attendance_records_method", "method IN ('qr_scan','manual_entry','auto_absent')");
                    table.CheckConstraint("ck_attendance_records_status", "status IN ('present','late','manual_audit','absent')");
                    table.ForeignKey(
                        name: "fk_attendance_records_attendance_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "attendance",
                        principalTable: "attendance_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_student_id_session_id",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "student_id", "session_id" });

            migrationBuilder.CreateIndex(
                name: "ux_attendance_records_session_student",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "session_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sessions_subject_group_id",
                schema: "attendance",
                table: "attendance_sessions",
                column: "subject_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sessions_term_id",
                schema: "attendance",
                table: "attendance_sessions",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ux_attendance_sessions_slot_date",
                schema: "attendance",
                table: "attendance_sessions",
                columns: new[] { "slot_id", "session_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_warnings_subject_group_id",
                schema: "attendance",
                table: "attendance_warnings",
                column: "subject_group_id");

            migrationBuilder.CreateIndex(
                name: "ux_attendance_warnings_student_group",
                schema: "attendance",
                table: "attendance_warnings",
                columns: new[] { "student_id", "subject_group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_change_requests_status_created_at",
                schema: "attendance",
                table: "device_change_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_device_change_requests_student_pending",
                schema: "attendance",
                table: "device_change_requests",
                column: "student_id",
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ux_registered_devices_student_active",
                schema: "attendance",
                table: "registered_devices",
                column: "student_id",
                unique: true,
                filter: "revoked_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "attendance_warnings",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "device_change_requests",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "registered_devices",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "attendance_sessions",
                schema: "attendance");
        }
    }
}
