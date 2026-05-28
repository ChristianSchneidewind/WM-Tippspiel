using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSpiel.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPositionAndAppearances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tipps_GameId",
                table: "Tipps");

            migrationBuilder.AddColumn<int>(
                name: "Appearances",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tipps_GameId_UserId",
                table: "Tipps",
                columns: new[] { "GameId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tipps_GameId_UserId",
                table: "Tipps");

            migrationBuilder.DropColumn(
                name: "Appearances",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Tipps_GameId",
                table: "Tipps",
                column: "GameId");
        }
    }
}
