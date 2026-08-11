using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalHintsUsedToPerformanceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalHintsUsed",
                table: "PerformanceProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalHintsUsed",
                table: "PerformanceProfiles");
        }
    }
}
