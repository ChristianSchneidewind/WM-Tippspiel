using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSpiel.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayTeamPenaltyScore",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamPenaltyScore",
                table: "Games",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayTeamPenaltyScore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HomeTeamPenaltyScore",
                table: "Games");
        }
    }
}
