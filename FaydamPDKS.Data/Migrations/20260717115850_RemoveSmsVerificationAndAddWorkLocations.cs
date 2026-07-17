using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSmsVerificationAndAddWorkLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "phone_verifications");

            migrationBuilder.AddColumn<int>(
                name: "correction_type",
                table: "attendance_correction_requests",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "attendance_correction_requests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "field_address",
                table: "attendance_correction_requests",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project_name",
                table: "attendance_correction_requests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "field_work_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    recurrence_type = table.Column<int>(type: "integer", nullable: false),
                    project_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    field_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_work_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_work_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_location_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_type = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    recurrence_type = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    project_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    field_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_location_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_location_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "field_work_request_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_work_request_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_work_request_days_field_work_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "field_work_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_location_assignment_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_location_assignment_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_location_assignment_days_work_location_assignments_ass~",
                        column: x => x.assignment_id,
                        principalTable: "work_location_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_field_work_request_days_request_id_day_of_week",
                table: "field_work_request_days",
                columns: new[] { "request_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_field_work_requests_user_id_start_date_end_date_status",
                table: "field_work_requests",
                columns: new[] { "user_id", "start_date", "end_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_location_assignment_days_assignment_id_day_of_week",
                table: "work_location_assignment_days",
                columns: new[] { "assignment_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_location_assignments_user_id_start_date_end_date_is_ac~",
                table: "work_location_assignments",
                columns: new[] { "user_id", "start_date", "end_date", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "field_work_request_days");

            migrationBuilder.DropTable(
                name: "work_location_assignment_days");

            migrationBuilder.DropTable(
                name: "field_work_requests");

            migrationBuilder.DropTable(
                name: "work_location_assignments");

            migrationBuilder.DropColumn(
                name: "correction_type",
                table: "attendance_correction_requests");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "attendance_correction_requests");

            migrationBuilder.DropColumn(
                name: "field_address",
                table: "attendance_correction_requests");

            migrationBuilder.DropColumn(
                name: "project_name",
                table: "attendance_correction_requests");

            migrationBuilder.CreateTable(
                name: "phone_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_phone_verifications", x => x.id));
            migrationBuilder.CreateIndex(name: "IX_phone_verifications_phone_number_created_at", table: "phone_verifications", columns: new[] { "phone_number", "created_at" });
        }
    }
}
