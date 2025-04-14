using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebGuide.Migrations
{
    /// <inheritdoc />
    public partial class AddLastReminderToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "Tasks");
        }
    }
}
