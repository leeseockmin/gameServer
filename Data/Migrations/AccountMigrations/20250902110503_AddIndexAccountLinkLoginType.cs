using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Data.Migrations.AccountMigrations
{
    /// <inheritdoc />
    public partial class AddIndexAccountLinkLoginType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ACCOUNT_LINX_ACCOUNT_ID",
                table: "accountlink");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_LINX_ACCOUNT_ID",
                table: "accountlink",
                columns: new[] { "accountId", "loginType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ACCOUNT_LINX_ACCOUNT_ID",
                table: "accountlink");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_LINX_ACCOUNT_ID",
                table: "accountlink",
                column: "accountId");
        }
    }
}
