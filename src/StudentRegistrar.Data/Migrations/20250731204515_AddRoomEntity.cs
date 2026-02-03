using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRegistrar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the Rooms table
            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name",
                unique: true);

            // Step 2: Add RoomId column to Courses
            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            // Step 3: Migrate existing Room data
            // Insert distinct room names from Courses.Room into Rooms table
            migrationBuilder.Sql(@"
                INSERT INTO ""Rooms"" (""Id"", ""Name"", ""Capacity"", ""RoomType"", ""CreatedAt"", ""UpdatedAt"")
                SELECT 
                    gen_random_uuid(),
                    ""Room"",
                    50,  -- Default capacity
                    1,   -- Default RoomType = Classroom
                    NOW(),
                    NOW()
                FROM (
                    SELECT DISTINCT ""Room""
                    FROM ""Courses""
                    WHERE ""Room"" IS NOT NULL AND ""Room"" != ''
                ) AS distinct_rooms
                ON CONFLICT (""Name"") DO NOTHING;
            ");

            // Step 4: Backfill RoomId based on Room name
            migrationBuilder.Sql(@"
                UPDATE ""Courses"" c
                SET ""RoomId"" = r.""Id""
                FROM ""Rooms"" r
                WHERE c.""Room"" = r.""Name"" AND c.""Room"" IS NOT NULL AND c.""Room"" != '';
            ");

            // Step 5: Now it's safe to drop the old Room column
            migrationBuilder.DropColumn(
                name: "Room",
                table: "Courses");

            // Step 6: Add foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_Courses_RoomId",
                table: "Courses",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Rooms_RoomId",
                table: "Courses",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Rooms_RoomId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Courses_RoomId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
