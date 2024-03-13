using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tweetr.Migrations
{
    /// <inheritdoc />
    public partial class AddedProfileImageInComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Comments");
        }
    }
}
