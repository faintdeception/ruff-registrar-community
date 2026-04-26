using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260208090000_AddEducatorInvitationFields")]
    public partial class AddEducatorInvitationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountHolderId",
                table: "Educators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeycloakUserId",
                table: "Educators",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Educators_AccountHolderId",
                table: "Educators",
                column: "AccountHolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Educators_TenantId_Email",
                table: "Educators",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Educators_TenantId_KeycloakUserId",
                table: "Educators",
                columns: new[] { "TenantId", "KeycloakUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Educators_AccountHolders_AccountHolderId",
                table: "Educators",
                column: "AccountHolderId",
                principalTable: "AccountHolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educators_AccountHolders_AccountHolderId",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Educators_AccountHolderId",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Educators_TenantId_Email",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Educators_TenantId_KeycloakUserId",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "AccountHolderId",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "KeycloakUserId",
                table: "Educators");
        }
    }
}
