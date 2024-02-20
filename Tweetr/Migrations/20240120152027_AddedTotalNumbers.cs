using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tweetr.Migrations
{
    /// <inheritdoc />
    public partial class AddedTotalNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalComments",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalLikes",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReposts",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalComments",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TotalLikes",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TotalReposts",
                table: "Posts");
        }
    }
}
