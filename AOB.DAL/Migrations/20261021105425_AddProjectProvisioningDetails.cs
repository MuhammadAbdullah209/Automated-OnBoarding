using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectProvisioningDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectProvisioningDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FlowId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProvisioningDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectProvisioningDetails_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProvisioningDetails_ProjectId",
                table: "ProjectProvisioningDetails",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectProvisioningDetails");
        }
    }
}
