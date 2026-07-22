using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationTypeToWorkLocationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "project_name",
                table: "field_work_requests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<int>(
                name: "location_type",
                table: "field_work_requests",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "location_type",
                table: "field_work_requests");

            migrationBuilder.AlterColumn<string>(
                name: "project_name",
                table: "field_work_requests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
