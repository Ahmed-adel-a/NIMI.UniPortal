using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentCodeCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_code_counters",
                schema: "users",
                columns: table => new
                {
                    counter_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_code_counters", x => x.counter_key);
                    table.CheckConstraint("ck_student_code_counters_last_value", "last_value >= 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_code_counters",
                schema: "users");
        }
    }
}
