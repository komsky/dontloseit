using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleaMarket.FrontEnd.Data.Migrations
{
    /// <inheritdoc />
    public partial class profileimage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageFileName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageFileName",
                table: "AspNetUsers");
        }
    }
}
