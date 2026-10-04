using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradehub_backend.Migrations
{
    /// <inheritdoc />
    public partial class MakePostProductOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Products_ProductId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Likes",
                table: "Posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "Posts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Products_ProductId",
                table: "Posts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Products_ProductId",
                table: "Posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "Posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Likes",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Products_ProductId",
                table: "Posts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
