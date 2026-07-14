using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceGlobalCalendarUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_work_calendar_days_workplace_id_date",
                table: "work_calendar_days");

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_days_date",
                table: "work_calendar_days",
                column: "date",
                unique: true,
                filter: "workplace_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_days_workplace_id_date",
                table: "work_calendar_days",
                columns: new[] { "workplace_id", "date" },
                unique: true,
                filter: "workplace_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_work_calendar_days_date",
                table: "work_calendar_days");

            migrationBuilder.DropIndex(
                name: "IX_work_calendar_days_workplace_id_date",
                table: "work_calendar_days");

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_days_workplace_id_date",
                table: "work_calendar_days",
                columns: new[] { "workplace_id", "date" },
                unique: true);
        }
    }
}
