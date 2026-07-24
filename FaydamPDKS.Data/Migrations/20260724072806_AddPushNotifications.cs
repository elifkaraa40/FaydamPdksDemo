using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "push_language",
                table: "device_sessions",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "push_platform",
                table: "device_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "push_token",
                table: "device_sessions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "push_token_disabled_at",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "push_token_updated_at",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "push_notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_push_notification_deliveries_device_sessions_device_session~",
                        column: x => x.device_session_id,
                        principalTable: "device_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_push_notification_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_sessions_push_token",
                table: "device_sessions",
                column: "push_token",
                unique: true,
                filter: "push_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_deliveries_device_session_id",
                table: "push_notification_deliveries",
                column: "device_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_deliveries_notification_id_device_session~",
                table: "push_notification_deliveries",
                columns: new[] { "notification_id", "device_session_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_deliveries_sent_at_next_attempt_at",
                table: "push_notification_deliveries",
                columns: new[] { "sent_at", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "push_notification_deliveries");

            migrationBuilder.DropIndex(
                name: "IX_device_sessions_push_token",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "push_language",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "push_platform",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "push_token",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "push_token_disabled_at",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "push_token_updated_at",
                table: "device_sessions");
        }
    }
}
