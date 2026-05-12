using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentRegistrar.Data;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    [DbContext(typeof(StudentRegistrarDbContext))]
    [Migration("20260512211000_RepairEducatorIsDeleted")]
    public partial class RepairEducatorIsDeleted : Migration
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
          AND table_name = 'Educators'
          AND column_name = 'IsDeleted') THEN
        ALTER TABLE ""Educators""
        ADD COLUMN ""IsDeleted"" boolean NOT NULL DEFAULT FALSE;
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
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Educators'
          AND column_name = 'IsDeleted') THEN
        ALTER TABLE ""Educators"" DROP COLUMN ""IsDeleted"";
    END IF;
END $$;");
        }
    }
}