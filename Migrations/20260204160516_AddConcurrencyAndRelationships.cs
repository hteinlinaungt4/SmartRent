using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRent.Migrations
{
    public partial class AddConcurrencyAndRelationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Properties",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Properties");
        }
    }
}
