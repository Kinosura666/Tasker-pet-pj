using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebGuide.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Reminder12hSent",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder24hSent",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder2hSent",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reminder12hSent",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Reminder24hSent",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Reminder2hSent",
                table: "Tasks");
        }
    }
}
