using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSessionSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "device_session_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "device_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    device_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    logged_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_sessions_user_id_device_id_hash_revoked_at",
                table: "device_sessions",
                columns: new[] { "user_id", "device_id_hash", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_device_sessions_user_id_revoked_at",
                table: "device_sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id",
                principalTable: "device_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "device_sessions");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_device_session_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "device_session_id",
                table: "refresh_tokens");
        }
    }
}
