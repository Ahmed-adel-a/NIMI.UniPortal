using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "fee_items",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_items", x => x.id);
                    table.CheckConstraint("ck_fee_items_amount", "amount > 0");
                    table.CheckConstraint("ck_fee_items_category", "category IN ('tuition','military_education','activity','other')");
                });

            migrationBuilder.CreateTable(
                name: "payment_intents",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_intents", x => x.id);
                    table.CheckConstraint("ck_payment_intents_amount", "amount > 0");
                    table.CheckConstraint("ck_payment_intents_status", "status IN ('pending','succeeded','failed','expired')");
                });

            migrationBuilder.CreateTable(
                name: "receipt_counters",
                schema: "payments",
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
                name: "student_fees",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_due = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_fees", x => x.id);
                    table.CheckConstraint("ck_student_fees_amount_due", "amount_due > 0");
                    table.CheckConstraint("ck_student_fees_amount_paid", "amount_paid >= 0 AND amount_paid <= amount_due");
                    table.CheckConstraint("ck_student_fees_status", "status IN ('pending','partially_paid','paid')");
                    table.ForeignKey(
                        name: "fk_student_fees_fee_items_fee_item_id",
                        column: x => x.fee_item_id,
                        principalSchema: "payments",
                        principalTable: "fee_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_fee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_payments_method", "method IN ('cash','bank_transfer','online')");
                    table.ForeignKey(
                        name: "fk_payments_payment_intents_payment_intent_id",
                        column: x => x.payment_intent_id,
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_student_fees_student_fee_id",
                        column: x => x.student_fee_id,
                        principalSchema: "payments",
                        principalTable: "student_fees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fee_items_department_id",
                schema: "payments",
                table: "fee_items",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_items_faculty_id",
                schema: "payments",
                table: "fee_items",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_items_is_active",
                schema: "payments",
                table: "fee_items",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_fee_items_term_id",
                schema: "payments",
                table: "fee_items",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_expires_at",
                schema: "payments",
                table: "payment_intents",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_fee_item_id",
                schema: "payments",
                table: "payment_intents",
                column: "fee_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_provider_provider_reference",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "provider", "provider_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_student_id",
                schema: "payments",
                table: "payment_intents",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_intent_id",
                schema: "payments",
                table: "payments",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_receipt_number",
                schema: "payments",
                table: "payments",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_student_fee_id",
                schema: "payments",
                table: "payments",
                column: "student_fee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_student_id_paid_at",
                schema: "payments",
                table: "payments",
                columns: new[] { "student_id", "paid_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_student_fees_fee_item_id_student_id",
                schema: "payments",
                table: "student_fees",
                columns: new[] { "fee_item_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_fees_student_id",
                schema: "payments",
                table: "student_fees",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "receipt_counters",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payment_intents",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "student_fees",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "fee_items",
                schema: "payments");
        }
    }
}
