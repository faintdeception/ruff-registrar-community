using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentRegistrar.Data;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260512204500_RepairCourseInstructorEducatorId")]
    public partial class RepairCourseInstructorEducatorId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'CourseInstructors'
          AND column_name = 'EducatorId') THEN
        ALTER TABLE ""CourseInstructors""
        ADD COLUMN ""EducatorId"" uuid NULL;
    END IF;
END $$;");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_CourseInstructors_EducatorId\" ON \"CourseInstructors\" (\"EducatorId\");");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_CourseInstructors_Educators_EducatorId') THEN
        ALTER TABLE ""CourseInstructors""
        ADD CONSTRAINT ""FK_CourseInstructors_Educators_EducatorId""
        FOREIGN KEY (""EducatorId"") REFERENCES ""Educators"" (""Id"");
    END IF;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_CourseInstructors_Educators_EducatorId') THEN
        ALTER TABLE ""CourseInstructors"" DROP CONSTRAINT ""FK_CourseInstructors_Educators_EducatorId"";
    END IF;
END $$;");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CourseInstructors_EducatorId\";");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'CourseInstructors'
          AND column_name = 'EducatorId') THEN
        ALTER TABLE ""CourseInstructors"" DROP COLUMN ""EducatorId"";
    END IF;
END $$;");
        }
    }
}