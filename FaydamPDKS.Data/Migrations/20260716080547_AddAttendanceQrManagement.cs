using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceQrManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "zones",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workplace_id",
                table: "zones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attendance_qr_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_legacy = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_qr_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_qr_codes_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_qr_codes_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_zones_workplace_id",
                table: "zones",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_qr_codes_token_hash",
                table: "attendance_qr_codes",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_qr_codes_workplace_id_zone_id_event_type_is_acti~",
                table: "attendance_qr_codes",
                columns: new[] { "workplace_id", "zone_id", "event_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_qr_codes_zone_id",
                table: "attendance_qr_codes",
                column: "zone_id");

            migrationBuilder.AddForeignKey(
                name: "FK_zones_workplaces_workplace_id",
                table: "zones",
                column: "workplace_id",
                principalTable: "workplaces",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_zones_workplaces_workplace_id",
                table: "zones");

            migrationBuilder.DropTable(
                name: "attendance_qr_codes");

            migrationBuilder.DropIndex(
                name: "IX_zones_workplace_id",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "workplace_id",
                table: "zones");
        }
    }
}
