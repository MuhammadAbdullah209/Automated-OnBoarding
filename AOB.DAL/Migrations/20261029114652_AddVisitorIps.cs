using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorIps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitorIps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Isp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GeoLookedUpAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HitCount = table.Column<long>(type: "bigint", nullable: false),
                    LastPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LastUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockedReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BlockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BlockedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorIps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorIps_IpAddress",
                table: "VisitorIps",
                column: "IpAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorIps_LastSeenAt",
                table: "VisitorIps",
                column: "LastSeenAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitorIps");
        }
    }
}
