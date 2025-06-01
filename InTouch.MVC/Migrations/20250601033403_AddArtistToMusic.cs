using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTouch.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistToMusic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "Musics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Artist",
                table: "Musics");
        }
    }
}
