using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_KeycloakId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Semesters_Code",
                table: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_EmailAddress",
                table: "AccountHolders");

            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_KeycloakUserId",
                table: "AccountHolders");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UserProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Students",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Semesters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Rooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "GradeRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Enrollments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Educators",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Courses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "CourseInstructors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CourseInstructors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AccountHolders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AcademicYears",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    SubscriptionTier = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionStatus = table.Column<int>(type: "integer", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LogoBase64 = table.Column<string>(type: "character varying(700000)", maxLength: 700000, nullable: true),
                    LogoMimeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ThemeConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    KeycloakRealm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AdminEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_KeycloakId",
                table: "Users",
                columns: new[] { "TenantId", "KeycloakId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_TenantId",
                table: "Students",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_TenantId",
                table: "Semesters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_TenantId_Code",
                table: "Semesters",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId",
                table: "Rooms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId_Name",
                table: "Rooms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_TenantId",
                table: "GradeRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_TenantId",
                table: "Enrollments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Educators_TenantId",
                table: "Educators",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TenantId",
                table: "Courses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructors_TenantId",
                table: "CourseInstructors",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_TenantId",
                table: "AccountHolders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_TenantId_EmailAddress",
                table: "AccountHolders",
                columns: new[] { "TenantId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_TenantId_KeycloakUserId",
                table: "AccountHolders",
                columns: new[] { "TenantId", "KeycloakUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_TenantId",
                table: "AcademicYears",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_TenantId_Name",
                table: "AcademicYears",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_KeycloakRealm",
                table: "Tenants",
                column: "KeycloakRealm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_KeycloakId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Students_TenantId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Semesters_TenantId",
                table: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_Semesters_TenantId_Code",
                table: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TenantId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TenantId_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_TenantId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_TenantId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Educators_TenantId",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Courses_TenantId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseInstructors_TenantId",
                table: "CourseInstructors");

            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_TenantId",
                table: "AccountHolders");

            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_TenantId_EmailAddress",
                table: "AccountHolders");

            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_TenantId_KeycloakUserId",
                table: "AccountHolders");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_TenantId",
                table: "AcademicYears");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_TenantId_Name",
                table: "AcademicYears");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "CourseInstructors");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CourseInstructors");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccountHolders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AcademicYears");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakId",
                table: "Users",
                column: "KeycloakId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_Code",
                table: "Semesters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_EmailAddress",
                table: "AccountHolders",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_KeycloakUserId",
                table: "AccountHolders",
                column: "KeycloakUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }
    }
}
