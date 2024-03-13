using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amorphie.token.Migrations
{
    /// <inheritdoc />
    public partial class deviceIdAddedToToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "Tokens",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Tokens");
        }
    }
}
