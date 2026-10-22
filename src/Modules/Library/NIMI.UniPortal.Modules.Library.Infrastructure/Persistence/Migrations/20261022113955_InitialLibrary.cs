using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "library");

            migrationBuilder.CreateTable(
                name: "books",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    isbn = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    authors = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    publisher = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    publication_year = table.Column<int>(type: "integer", nullable: true),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_books", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loan_policies",
                schema: "library",
                columns: table => new
                {
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    loan_days = table.Column<int>(type: "integer", nullable: false),
                    max_open_loans = table.Column<int>(type: "integer", nullable: false),
                    max_renewals = table.Column<int>(type: "integer", nullable: false),
                    hold_days = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_policies", x => x.role);
                    table.CheckConstraint("ck_loan_policies_role", "role IN ('student','professor')");
                    table.CheckConstraint("ck_loan_policies_values", "loan_days > 0 AND max_open_loans > 0 AND max_renewals >= 0 AND hold_days > 0");
                });

            migrationBuilder.CreateTable(
                name: "book_copies",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shelf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    held_for_reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_book_copies", x => x.id);
                    table.CheckConstraint("ck_book_copies_status", "status IN ('available','on_loan','on_hold','lost','withdrawn')");
                    table.ForeignKey(
                        name: "fk_book_copies_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "library",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    copy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.CheckConstraint("ck_reservations_status", "status IN ('waiting','ready','fulfilled','cancelled','expired')");
                    table.ForeignKey(
                        name: "fk_reservations_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "library",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    copy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    borrowed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    returned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    renewals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.CheckConstraint("ck_loans_renewals", "renewals >= 0");
                    table.ForeignKey(
                        name: "fk_loans_book_copies_copy_id",
                        column: x => x.copy_id,
                        principalSchema: "library",
                        principalTable: "book_copies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loans_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "library",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_reminders",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_on = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_reminders", x => x.id);
                    table.CheckConstraint("ck_loan_reminders_kind", "kind IN ('due_soon','overdue')");
                    table.ForeignKey(
                        name: "fk_loan_reminders_loans_loan_id",
                        column: x => x.loan_id,
                        principalSchema: "library",
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_book_copies_barcode",
                schema: "library",
                table: "book_copies",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_copies_book_id_status",
                schema: "library",
                table: "book_copies",
                columns: new[] { "book_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_books_faculty_id",
                schema: "library",
                table: "books",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_books_isbn",
                schema: "library",
                table: "books",
                column: "isbn",
                unique: true,
                filter: "isbn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_books_title",
                schema: "library",
                table: "books",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_loan_reminders_loan_id_kind_sent_on",
                schema: "library",
                table: "loan_reminders",
                columns: new[] { "loan_id", "kind", "sent_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loans_book_id",
                schema: "library",
                table: "loans",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_due_at",
                schema: "library",
                table: "loans",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "ix_loans_user_id_returned_at",
                schema: "library",
                table: "loans",
                columns: new[] { "user_id", "returned_at" });

            migrationBuilder.CreateIndex(
                name: "ux_loans_open_copy",
                schema: "library",
                table: "loans",
                column: "copy_id",
                unique: true,
                filter: "returned_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_book_id_status_created_at",
                schema: "library",
                table: "reservations",
                columns: new[] { "book_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_user_id",
                schema: "library",
                table: "reservations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_reservations_active_book_user",
                schema: "library",
                table: "reservations",
                columns: new[] { "book_id", "user_id" },
                unique: true,
                filter: "status IN ('waiting','ready')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_policies",
                schema: "library");

            migrationBuilder.DropTable(
                name: "loan_reminders",
                schema: "library");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "library");

            migrationBuilder.DropTable(
                name: "loans",
                schema: "library");

            migrationBuilder.DropTable(
                name: "book_copies",
                schema: "library");

            migrationBuilder.DropTable(
                name: "books",
                schema: "library");
        }
    }
}
