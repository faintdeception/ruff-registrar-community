using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantStripeConnectFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectChargesEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectDetailsSubmitted",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeConnectOnboardingCompletedAtUtc",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectPayoutsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectChargesEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectDetailsSubmitted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectOnboardingCompletedAtUtc",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeConnectPayoutsEnabled",
                table: "Tenants");
        }
    }
}
