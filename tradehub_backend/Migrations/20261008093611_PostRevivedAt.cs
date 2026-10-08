using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradehub_backend.Migrations
{
    /// <inheritdoc />
    public partial class PostRevivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevivedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevivedAt",
                table: "Posts");
        }
    }
}
