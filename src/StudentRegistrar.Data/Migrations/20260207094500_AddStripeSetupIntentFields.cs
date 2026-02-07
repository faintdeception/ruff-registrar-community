using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260207094500_AddStripeSetupIntentFields")]
    /// <inheritdoc />
    public partial class AddStripeSetupIntentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripePaymentMethodId",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSetupIntentId",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePaymentMethodId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeSetupIntentId",
                table: "Tenants");
        }
    }
}
