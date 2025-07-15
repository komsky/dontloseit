using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleaMarket.FrontEnd.Data.Migrations
{
    /// <inheritdoc />
    public partial class deadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "Items",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "Items");
        }
    }
}
