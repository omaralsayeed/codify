using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds ToolsUsedJson and ReasoningSummary columns to HintLogs so the agentic
/// Tutor Agent can persist evidence of its tool-calling decisions.
/// </summary>
public partial class AddHintLogAgentFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ToolsUsedJson",
            table: "HintLogs",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReasoningSummary",
            table: "HintLogs",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ReasoningSummary", "HintLogs");
        migrationBuilder.DropColumn("ToolsUsedJson", "HintLogs");
    }
}
