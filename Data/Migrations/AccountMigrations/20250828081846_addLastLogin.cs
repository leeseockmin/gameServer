using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Data.Migrations.AccountMigrations
{
    /// <inheritdoc />
    public partial class addLastLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lastLoginTime",
                table: "account",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastLoginTime",
                table: "account");
        }
    }
}
