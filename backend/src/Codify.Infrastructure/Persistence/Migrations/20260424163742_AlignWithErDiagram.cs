using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithErDiagram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Users",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SolvedProblems",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TestCases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TestCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "TestCases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TestCases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PassedTestCases",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "Submissions",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTestCases",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Submissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "AcceptedSubmissionsCount",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "Problems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Problems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Problems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MemoryLimitMb",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 256);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Problems",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMs",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 2000);

            migrationBuilder.AddColumn<int>(
                name: "TotalSubmissionsCount",
                table: "Problems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Problems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ConceptTags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ConceptTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ConceptTags",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ConceptTags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Problems_AuthorId",
                table: "Problems",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Slug",
                table: "Problems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Slug",
                table: "ConceptTags",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Problems_Users_AuthorId",
                table: "Problems",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Problems_Users_AuthorId",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_Problems_AuthorId",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_Problems_Slug",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_ConceptTags_Slug",
                table: "ConceptTags");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SolvedProblems",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "PassedTestCases",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TotalTestCases",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AcceptedSubmissionsCount",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "MemoryLimitMb",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "TimeLimitMs",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "TotalSubmissionsCount",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ConceptTags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ConceptTags");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ConceptTags");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ConceptTags");
        }
    }
}
