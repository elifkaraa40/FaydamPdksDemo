using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileAccessEventIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "device_event_id",
                table: "access_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "access_logs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Terminal");

            migrationBuilder.CreateIndex(
                name: "IX_access_logs_device_event_id",
                table: "access_logs",
                column: "device_event_id",
                unique: true,
                filter: "device_event_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_access_logs_device_event_id",
                table: "access_logs");

            migrationBuilder.DropColumn(
                name: "device_event_id",
                table: "access_logs");

            migrationBuilder.DropColumn(
                name: "source",
                table: "access_logs");
        }
    }
}
