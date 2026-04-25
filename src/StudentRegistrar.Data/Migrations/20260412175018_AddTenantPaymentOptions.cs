using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPaymentOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentOptionsJson",
                table: "Tenants",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentOptionsJson",
                table: "Tenants");
        }
    }
}
