using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLatencyMsConfidenceAndAdminEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ModelUsed",
                table: "HintLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LatencyMs",
                table: "HintLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "FeedbackRecords",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "HintLogs");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "FeedbackRecords");

            migrationBuilder.AlterColumn<string>(
                name: "ModelUsed",
                table: "HintLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
