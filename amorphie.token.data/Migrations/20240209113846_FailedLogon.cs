using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amorphie.token.Migrations
{
    /// <inheritdoc />
    public partial class FailedLogon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Logon",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FailedLogon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    LogonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedLogon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FailedLogon_Logon_LogonId",
                        column: x => x.LogonId,
                        principalTable: "Logon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedLogon_LogonId",
                table: "FailedLogon",
                column: "LogonId");

            migrationBuilder.CreateIndex(
                name: "IX_FailedLogon_Reference",
                table: "FailedLogon",
                column: "Reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedLogon");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Logon");
        }
    }
}
