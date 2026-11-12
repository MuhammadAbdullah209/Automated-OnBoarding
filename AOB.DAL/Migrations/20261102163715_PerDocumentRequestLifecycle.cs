using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PerDocumentRequestLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Item statuses are stored as text: Requested is now Pending and ChangesRequested is now Rejected. A rejected
            // item the customer already answered (with a note) was waiting for review, so it becomes Resubmitted.
            migrationBuilder.Sql(@"
UPDATE ProjectInformationRequestItems SET Status = 'Pending' WHERE Status = 'Requested';
UPDATE i SET i.Status = 'Resubmitted'
FROM ProjectInformationRequestItems i
JOIN ProjectInformationRequests r ON r.Id = i.RequestId
WHERE i.Status = 'ChangesRequested' AND r.CustomerRespondedAt IS NOT NULL;
UPDATE ProjectInformationRequestItems SET Status = 'Rejected' WHERE Status = 'ChangesRequested';");

            migrationBuilder.DropColumn(
                name: "ReviewRequestType",
                table: "ProjectInformationRequestItems");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNote",
                table: "ProjectInformationRequestItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "ProjectInformationRequestItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectInformationResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInformationResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInformationResponses_ProjectInformationRequestItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ProjectInformationRequestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInformationResponses_ItemId_SubmittedAt",
                table: "ProjectInformationResponses",
                columns: new[] { "ItemId", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInformationResponses");

            migrationBuilder.DropColumn(
                name: "CustomerNote",
                table: "ProjectInformationRequestItems");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "ProjectInformationRequestItems");

            migrationBuilder.AddColumn<string>(
                name: "ReviewRequestType",
                table: "ProjectInformationRequestItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ProjectInformationRequestItems SET Status = 'Requested' WHERE Status = 'Pending';
UPDATE ProjectInformationRequestItems SET Status = 'ChangesRequested' WHERE Status IN ('Rejected', 'Resubmitted');
UPDATE ProjectInformationRequestItems SET Status = 'Approved' WHERE Status = 'Resolved';");
        }
    }
}
