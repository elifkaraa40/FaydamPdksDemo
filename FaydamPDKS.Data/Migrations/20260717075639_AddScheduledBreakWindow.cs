using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledBreakWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "scheduled_break_end",
                table: "shifts",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "scheduled_break_start",
                table: "shifts",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE shifts
                SET scheduled_break_start = TIME '12:30', scheduled_break_end = TIME '13:30'
                WHERE starts_at <= TIME '12:30' AND ends_at >= TIME '13:30' AND break_minutes = 60;

                UPDATE shifts
                SET name = 'Standart 08:30-18:00', starts_at = TIME '08:30'
                WHERE name = 'Standart 09:00-18:00' AND starts_at = TIME '09:00' AND ends_at = TIME '18:00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scheduled_break_end",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "scheduled_break_start",
                table: "shifts");
        }
    }
}
