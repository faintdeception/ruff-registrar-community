using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentRegistrar.Data;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260511195500_MakeSemesterRegistrationDatesOptional")]
    public partial class MakeSemesterRegistrationDatesOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RegistrationEndDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegistrationStartDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Semesters\" SET \"RegistrationStartDate\" = \"StartDate\" WHERE \"RegistrationStartDate\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"Semesters\" SET \"RegistrationEndDate\" = \"EndDate\" WHERE \"RegistrationEndDate\" IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegistrationEndDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegistrationStartDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}