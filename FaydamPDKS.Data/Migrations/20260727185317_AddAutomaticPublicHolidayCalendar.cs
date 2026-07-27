using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticPublicHolidayCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_half_day",
                table: "work_calendar_days",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_generated",
                table: "work_calendar_days",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "work_calendar_days",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "source_updated_at",
                table: "work_calendar_days",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "holiday_calendar_sync_states",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_successful_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    warning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_calendar_sync_states", x => x.year);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holiday_calendar_sync_states");

            migrationBuilder.DropColumn(
                name: "is_half_day",
                table: "work_calendar_days");

            migrationBuilder.DropColumn(
                name: "is_system_generated",
                table: "work_calendar_days");

            migrationBuilder.DropColumn(
                name: "source",
                table: "work_calendar_days");

            migrationBuilder.DropColumn(
                name: "source_updated_at",
                table: "work_calendar_days");
        }
    }
}
