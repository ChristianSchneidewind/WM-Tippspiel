using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSpiel.Migrations
{
    /// <inheritdoc />
    public partial class AddIfesIdToGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IfesId",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IfesId",
                table: "Games");
        }
    }
}
