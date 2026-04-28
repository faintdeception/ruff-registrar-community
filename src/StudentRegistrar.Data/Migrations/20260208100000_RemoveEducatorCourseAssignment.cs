using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260208100000_RemoveEducatorCourseAssignment")]
    public partial class RemoveEducatorCourseAssignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators");

            migrationBuilder.DropIndex(
                name: "IX_Educators_CourseId",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Educators");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "Educators");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Educators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "Educators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Educators_CourseId",
                table: "Educators",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Educators_Courses_CourseId",
                table: "Educators",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
