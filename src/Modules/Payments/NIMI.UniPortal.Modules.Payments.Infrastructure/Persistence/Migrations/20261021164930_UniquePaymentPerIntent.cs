using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniquePaymentPerIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_payment_intent_id",
                schema: "payments",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_intent_id",
                schema: "payments",
                table: "payments",
                column: "payment_intent_id",
                unique: true,
                filter: "payment_intent_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_payment_intent_id",
                schema: "payments",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_intent_id",
                schema: "payments",
                table: "payments",
                column: "payment_intent_id");
        }
    }
}
