using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneAuthenticationAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "account_status",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "phone_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_verifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_PhoneNumber",
                table: "users",
                column: "PhoneNumber",
                unique: true,
                filter: "\"PhoneNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_phone_verifications_phone_number_created_at",
                table: "phone_verifications",
                columns: new[] { "phone_number", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phone_verifications");

            migrationBuilder.DropIndex(
                name: "IX_users_PhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "account_status",
                table: "users");
        }
    }
}
