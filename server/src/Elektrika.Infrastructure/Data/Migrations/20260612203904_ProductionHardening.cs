using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elektrika.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductionHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "OrderRequests",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TelegramAttempts",
                table: "OrderRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TelegramNextRetryAtUtc",
                table: "OrderRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TelegramSentAtUtc",
                table: "OrderRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TelegramStatus",
                table: "OrderRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequests_TelegramStatus",
                table: "OrderRequests",
                column: "TelegramStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderRequests_TelegramStatus",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "TelegramAttempts",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "TelegramNextRetryAtUtc",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "TelegramSentAtUtc",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "TelegramStatus",
                table: "OrderRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "OrderRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
