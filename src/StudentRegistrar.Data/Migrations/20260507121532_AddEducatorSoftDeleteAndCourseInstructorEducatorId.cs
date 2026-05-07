using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEducatorSoftDeleteAndCourseInstructorEducatorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Educators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EducatorId",
                table: "CourseInstructors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructors_EducatorId",
                table: "CourseInstructors",
                column: "EducatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseInstructors_Educators_EducatorId",
                table: "CourseInstructors",
                column: "EducatorId",
                principalTable: "Educators",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseInstructors_Educators_EducatorId",
                table: "CourseInstructors");

            migrationBuilder.DropIndex(
                name: "IX_CourseInstructors_EducatorId",
                table: "CourseInstructors");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "EducatorId",
                table: "CourseInstructors");
        }
    }
}
