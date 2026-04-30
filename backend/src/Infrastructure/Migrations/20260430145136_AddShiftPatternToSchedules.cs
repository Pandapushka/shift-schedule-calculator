using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftPatternToSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DayHours",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: true,
                defaultValue: "08:00-20:00");

            migrationBuilder.AddColumn<string>(
                name: "NightHours",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: true,
                defaultValue: "20:00-08:00");

            migrationBuilder.AddColumn<string>(
                name: "ShiftPattern",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayHours",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "NightHours",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "ShiftPattern",
                table: "ShiftSchedules");
        }
    }
}
