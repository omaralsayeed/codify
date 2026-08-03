using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds the Analytics Agent columns to the PerformanceProfiles table.
/// Backward compatible — all new columns have defaults.
/// </summary>
public partial class AddAnalyticsProfileColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LearningStage",
            table: "PerformanceProfiles",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Beginner");

        migrationBuilder.AddColumn<int>(
            name: "OverallScore",
            table: "PerformanceProfiles",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Consistency",
            table: "PerformanceProfiles",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Low");

        migrationBuilder.AddColumn<float>(
            name: "Confidence",
            table: "PerformanceProfiles",
            type: "real",
            nullable: false,
            defaultValue: 0f);

        migrationBuilder.AddColumn<string>(
            name: "RecommendedDifficulty",
            table: "PerformanceProfiles",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Easy");

        migrationBuilder.AddColumn<string>(
            name: "AnalyticsJson",
            table: "PerformanceProfiles",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "{}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("AnalyticsJson", "PerformanceProfiles");
        migrationBuilder.DropColumn("RecommendedDifficulty", "PerformanceProfiles");
        migrationBuilder.DropColumn("Confidence", "PerformanceProfiles");
        migrationBuilder.DropColumn("Consistency", "PerformanceProfiles");
        migrationBuilder.DropColumn("OverallScore", "PerformanceProfiles");
        migrationBuilder.DropColumn("LearningStage", "PerformanceProfiles");
    }
}
