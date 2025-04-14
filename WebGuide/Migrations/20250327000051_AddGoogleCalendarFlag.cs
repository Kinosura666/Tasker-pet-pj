using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebGuide.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AddToGoogleCalendar",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddToGoogleCalendar",
                table: "Tasks");
        }
    }
}
