using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDocuments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DocumentTypes",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "IsRequired", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Signed DPA between your organisation and PCI Pal", true, false, "Data Processing Agreement", 10, null },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A brief overview of your organisation and payment operations", true, false, "Business Description / Company Overview", 20, null },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Diagram showing how your systems connect to PCI Pal endpoints", true, false, "Network Topology Diagram", 30, null },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Most recent SAQ or equivalent PCI compliance evidence", true, false, "PCI DSS Self-Assessment Questionnaire (SAQ)", 40, null },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Official company registration documentation", true, false, "Certificate of Incorporation", 50, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_CustomerId_DocumentTypeId",
                table: "CustomerDocuments",
                columns: new[] { "CustomerId", "DocumentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDocuments_DocumentTypeId",
                table: "CustomerDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_Name",
                table: "DocumentTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDocuments");

            migrationBuilder.DropTable(
                name: "DocumentTypes");
        }
    }
}
