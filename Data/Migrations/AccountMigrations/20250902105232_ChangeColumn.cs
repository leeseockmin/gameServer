using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Data.Migrations.AccountMigrations
{
    /// <inheritdoc />
    public partial class ChangeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ACCOUNT_USER_ID",
                table: "account");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "userId",
                table: "account",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_USER_ID",
                table: "account",
                column: "userId");
        }
    }
}
