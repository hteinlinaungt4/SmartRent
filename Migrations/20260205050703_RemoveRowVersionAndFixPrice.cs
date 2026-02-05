using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRent.Migrations
{
    public partial class RemoveRowVersionAndFixPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Properties\" DROP COLUMN IF EXISTS \"RowVersion\";");
            migrationBuilder.Sql("ALTER TABLE \"Properties\" ALTER COLUMN \"Price\" TYPE numeric USING \"Price\"::numeric;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Properties\" ALTER COLUMN \"Price\" TYPE text;");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Properties",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
