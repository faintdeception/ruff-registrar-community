using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountHolderToCourseInstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountHolderId",
                table: "CourseInstructors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructors_AccountHolderId",
                table: "CourseInstructors",
                column: "AccountHolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseInstructors_AccountHolders_AccountHolderId",
                table: "CourseInstructors",
                column: "AccountHolderId",
                principalTable: "AccountHolders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseInstructors_AccountHolders_AccountHolderId",
                table: "CourseInstructors");

            migrationBuilder.DropIndex(
                name: "IX_CourseInstructors_AccountHolderId",
                table: "CourseInstructors");

            migrationBuilder.DropColumn(
                name: "AccountHolderId",
                table: "CourseInstructors");
        }
    }
}
