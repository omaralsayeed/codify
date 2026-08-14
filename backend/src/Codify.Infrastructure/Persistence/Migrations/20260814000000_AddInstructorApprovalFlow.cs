using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Status column — default to 'Active' so all existing users remain active
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Active");

            // Add Organization column (optional, supplied by instructors at registration)
            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "Users",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            // Add approval audit columns
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReviewedAt",    table: "Users");
            migrationBuilder.DropColumn(name: "ReviewedBy",    table: "Users");
            migrationBuilder.DropColumn(name: "Organization",  table: "Users");
            migrationBuilder.DropColumn(name: "Status",        table: "Users");
        }
    }
}
