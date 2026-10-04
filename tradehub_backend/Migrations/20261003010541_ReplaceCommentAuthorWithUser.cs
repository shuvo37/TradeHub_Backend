using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradehub_backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCommentAuthorWithUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorAvatar",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Comments");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_UserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "AuthorAvatar",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
