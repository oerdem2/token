using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amorphie.token.Migrations
{
    /// <inheritdoc />
    public partial class TokenInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedTokenId",
                table: "Tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenType",
                table: "Tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedTokenId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "TokenType",
                table: "Tokens");
        }
    }
}
