using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elektrika.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClientRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "OrderRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequests_ClientRequestId",
                table: "OrderRequests",
                column: "ClientRequestId",
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderRequests_ClientRequestId",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "OrderRequests");
        }
    }
}
