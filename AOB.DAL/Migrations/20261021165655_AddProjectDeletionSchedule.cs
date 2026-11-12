using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDeletionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletionScheduledFor",
                table: "Projects",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeletionScheduledFor",
                table: "Projects",
                column: "DeletionScheduledFor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_DeletionScheduledFor",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletionScheduledFor",
                table: "Projects");
        }
    }
}
