using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "break_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    start_device_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    end_device_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    auto_closed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_break_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_break_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_break_records_end_device_event_id",
                table: "break_records",
                column: "end_device_event_id",
                unique: true,
                filter: "end_device_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_break_records_start_device_event_id",
                table: "break_records",
                column: "start_device_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_break_records_user_id_ended_at",
                table: "break_records",
                columns: new[] { "user_id", "ended_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "break_records");
        }
    }
}
