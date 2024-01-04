using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amorphie.token.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJwt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Jwt",
                table: "Tokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Tokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsentId",
                table: "Tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ConsentId",
                table: "Tokens",
                column: "ConsentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Reference",
                table: "Tokens",
                column: "Reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tokens_ConsentId",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Reference",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "ConsentId",
                table: "Tokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Tokens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Jwt",
                table: "Tokens",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
