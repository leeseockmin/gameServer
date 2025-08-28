using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace DB.Data.Migrations.AccountMigrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    accountId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    deviceId = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    osType = table.Column<int>(type: "int", nullable: false),
                    loginType = table.Column<int>(type: "int", nullable: false),
                    createDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.accountId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "accountlink",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    accessToken = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    loginType = table.Column<int>(type: "int", nullable: false),
                    accountId = table.Column<long>(type: "bigint", nullable: false),
                    createDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountlink", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_USER_ID",
                table: "account",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_LINK_LOGIN_ACCESS",
                table: "accountlink",
                columns: new[] { "loginType", "accessToken" });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_LINX_ACCOUNT_ID",
                table: "accountlink",
                column: "accountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "accountlink");
        }
    }
}
