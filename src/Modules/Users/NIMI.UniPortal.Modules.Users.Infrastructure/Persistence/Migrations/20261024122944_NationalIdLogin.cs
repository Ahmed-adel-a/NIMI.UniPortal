using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NationalIdLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_users_national_id",
                schema: "users",
                table: "users",
                column: "national_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_national_id",
                schema: "users",
                table: "users");
        }
    }
}
