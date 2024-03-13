using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amorphie.token.Migrations
{
    /// <inheritdoc />
    public partial class LogonEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Logon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogonType = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    WorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    LastJobKey = table.Column<long>(type: "bigint", nullable: false),
                    LogonStatus = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logon", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Logon_Reference",
                table: "Logon",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Logon_WorkflowInstanceId",
                table: "Logon",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logon");
        }
    }
}
