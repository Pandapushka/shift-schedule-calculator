using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalary",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSalary",
                table: "ShiftSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Overtimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShiftScheduleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OvertimeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Hours = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Overtimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Overtimes_ShiftSchedules_ShiftScheduleId",
                        column: x => x.ShiftScheduleId,
                        principalTable: "ShiftSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Overtimes_ShiftScheduleId",
                table: "Overtimes",
                column: "ShiftScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Overtimes");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "TotalSalary",
                table: "ShiftSchedules");
        }
    }
}
