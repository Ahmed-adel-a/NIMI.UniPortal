using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentHeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                schema: "users",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ux_users_active_department_head",
                schema: "users",
                table: "users",
                column: "department_id",
                unique: true,
                filter: "role = 'department_head' AND is_active");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                schema: "users",
                table: "users",
                sql: "role IN ('student', 'professor', 'department_head', 'admin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_active_department_head",
                schema: "users",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                schema: "users",
                table: "users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                schema: "users",
                table: "users",
                sql: "role IN ('student', 'professor', 'admin')");
        }
    }
}
