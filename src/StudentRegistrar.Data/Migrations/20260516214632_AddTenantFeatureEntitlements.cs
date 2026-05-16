using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFeatureEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantBrandingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LogoBase64 = table.Column<string>(type: "character varying(700000)", maxLength: 700000, nullable: true),
                    LogoMimeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FooterText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HidePoweredBy = table.Column<bool>(type: "boolean", nullable: false),
                    CustomCss = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrandingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantFeatureOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFeatureOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrandingSettings_TenantId",
                table: "TenantBrandingSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatureOverrides_TenantId",
                table: "TenantFeatureOverrides",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatureOverrides_TenantId_FeatureKey",
                table: "TenantFeatureOverrides",
                columns: new[] { "TenantId", "FeatureKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBrandingSettings");

            migrationBuilder.DropTable(
                name: "TenantFeatureOverrides");
        }
    }
}
