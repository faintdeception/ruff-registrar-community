using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantTrialBillingHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAtUtc",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrialEndsAtUtc",
                table: "Tenants");
        }
    }
}
