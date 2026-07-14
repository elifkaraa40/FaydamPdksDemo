using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaydamPDKS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workplace_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workplaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workplaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_departments_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Mevcut serbest metin bölüm verisini kaybetmeden normalize organizasyon yapısına taşır.
            migrationBuilder.Sql("""
                INSERT INTO workplaces (id, code, name, time_zone_id, address, is_active)
                VALUES ('00000000-0000-0000-0000-000000000001', 'MERKEZ', 'Merkez İşyeri', 'Europe/Istanbul', NULL, TRUE);

                INSERT INTO departments (id, workplace_id, code, name, is_active)
                SELECT md5('department:' || department)::uuid,
                       '00000000-0000-0000-0000-000000000001',
                       'LEG-' || UPPER(SUBSTRING(md5(department), 1, 8)),
                       department,
                       TRUE
                FROM (SELECT DISTINCT department FROM users WHERE department IS NOT NULL AND BTRIM(department) <> '') legacy;

                UPDATE users
                SET workplace_id = '00000000-0000-0000-0000-000000000001',
                    department_id = CASE WHEN department IS NULL OR BTRIM(department) = ''
                                         THEN NULL ELSE md5('department:' || department)::uuid END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_workplace_id",
                table: "users",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_workplace_id_code",
                table: "departments",
                columns: new[] { "workplace_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workplaces_code",
                table: "workplaces",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_departments_department_id",
                table: "users",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_workplaces_workplace_id",
                table: "users",
                column: "workplace_id",
                principalTable: "workplaces",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_departments_department_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_workplaces_workplace_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "workplaces");

            migrationBuilder.DropIndex(
                name: "IX_users_department_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_workplace_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "workplace_id",
                table: "users");
        }
    }
}
