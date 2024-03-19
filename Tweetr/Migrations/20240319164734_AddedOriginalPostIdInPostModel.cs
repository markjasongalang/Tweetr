using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tweetr.Migrations
{
    /// <inheritdoc />
    public partial class AddedOriginalPostIdInPostModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalPostId",
                table: "Posts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalPostId",
                table: "Posts");
        }
    }
}
