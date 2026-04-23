using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSpiel.Migrations
{
    /// <inheritdoc />
    public partial class InitialApiStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayTeam",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HomeTeam",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamId",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamId",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games",
                column: "HomeTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_AwayTeamId",
                table: "Games",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_HomeTeamId",
                table: "Games",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_AwayTeamId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_HomeTeamId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AwayTeamId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "AwayTeam",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeTeam",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
