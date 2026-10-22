using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "forum");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "threads",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author_rank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    author_department = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author_department_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    reply_count = table.Column<int>(type: "integer", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    author_notified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    author_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_threads", x => x.id);
                    table.CheckConstraint("ck_threads_reply_count", "reply_count >= 0");
                    table.ForeignKey(
                        name: "fk_threads_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "forum",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author_rank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    author_department = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author_department_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_threads_thread_id",
                        column: x => x.thread_id,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: true),
                    post_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reported_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.CheckConstraint("ck_reports_one_target", "(thread_id IS NULL) <> (post_id IS NULL)");
                    table.CheckConstraint("ck_reports_resolution", "resolution IS NULL OR resolution IN ('removed','dismissed')");
                    table.CheckConstraint("ck_reports_status", "status IN ('open','resolved')");
                    table.ForeignKey(
                        name: "fk_reports_posts_post_id",
                        column: x => x.post_id,
                        principalSchema: "forum",
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reports_threads_thread_id",
                        column: x => x.thread_id,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active_faculty_id",
                schema: "forum",
                table: "categories",
                columns: new[] { "is_active", "faculty_id" });

            migrationBuilder.CreateIndex(
                name: "ix_posts_author_id",
                schema: "forum",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_created_at",
                schema: "forum",
                table: "posts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_posts_thread_id_created_at",
                schema: "forum",
                table: "posts",
                columns: new[] { "thread_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_post_id",
                schema: "forum",
                table: "reports",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status_created_at",
                schema: "forum",
                table: "reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_thread_id",
                schema: "forum",
                table: "reports",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_author_id",
                schema: "forum",
                table: "threads",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_category_id_is_pinned_last_activity_at",
                schema: "forum",
                table: "threads",
                columns: new[] { "category_id", "is_pinned", "last_activity_at" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_created_at",
                schema: "forum",
                table: "threads",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "posts",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "threads",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "forum");
        }
    }
}
