using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Calinout_Project.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalCarbFatProteinCaloriesWeightToDailyLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalCalories",
                table: "DailyLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCarbs",
                table: "DailyLogs",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFat",
                table: "DailyLogs",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFoodWeight",
                table: "DailyLogs",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProtein",
                table: "DailyLogs",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCalories",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "TotalCarbs",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "TotalFat",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "TotalFoodWeight",
                table: "DailyLogs");

            migrationBuilder.DropColumn(
                name: "TotalProtein",
                table: "DailyLogs");
        }
    }
}