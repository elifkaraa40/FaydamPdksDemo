using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveDayPortion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "day_portion",
                table: "leave_requests",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "day_portion",
                table: "leave_requests");
        }
    }
}
