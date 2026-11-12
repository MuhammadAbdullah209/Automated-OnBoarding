using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class MakeDocumentTypeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerDocuments_CustomerId_DocumentTypeId",
                table: "CustomerDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "CustomerDocuments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_CustomerId_DocumentTypeId",
                table: "CustomerDocuments",
                columns: new[] { "CustomerId", "DocumentTypeId" },
                unique: true,
                filter: "[DocumentTypeId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerDocuments_CustomerId_DocumentTypeId",
                table: "CustomerDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "CustomerDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_CustomerId_DocumentTypeId",
                table: "CustomerDocuments",
                columns: new[] { "CustomerId", "DocumentTypeId" },
                unique: true);
        }
    }
}
