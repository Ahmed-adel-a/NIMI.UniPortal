using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActiveTermDeferrable : Migration
    {
        /// <summary>
        /// Rule R1 without depending on the order PostgreSQL applies the two UPDATEs of one save: the exclusion
        /// constraint is checked when the transaction commits, so closing the old term and activating the new one
        /// may happen in either order. A partial unique index is checked per row and failed when the activated
        /// term's id sorted before the closed one's.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX scheduling.ix_terms_status;");
            migrationBuilder.Sql(
                "ALTER TABLE scheduling.terms ADD CONSTRAINT ex_terms_one_active " +
                "EXCLUDE USING btree (status WITH =) WHERE (status = 'active') DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE scheduling.terms DROP CONSTRAINT ex_terms_one_active;");
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_terms_status ON scheduling.terms (status) WHERE status = 'active';");
        }
    }
}
