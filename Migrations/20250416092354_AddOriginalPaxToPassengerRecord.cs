using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyToursApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalPaxToPassengerRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalPax",
                table: "PassengerRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalPax",
                table: "PassengerRecords");
        }
    }
}
