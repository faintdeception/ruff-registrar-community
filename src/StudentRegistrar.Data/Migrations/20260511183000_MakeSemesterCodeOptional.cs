using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentRegistrar.Data;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260511183000_MakeSemesterCodeOptional")]
    public partial class MakeSemesterCodeOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Semesters\" SET \"Code\" = NULL WHERE btrim(coalesce(\"Code\", '')) = '';");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Semesters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Semesters\" SET \"Code\" = 'SEMESTER' || substring(md5(\"Id\"::text) from 1 for 8) WHERE \"Code\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Semesters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}