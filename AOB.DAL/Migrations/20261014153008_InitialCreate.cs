using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalUsers_UserAuths_UserAuthId",
                        column: x => x.UserAuthId,
                        principalTable: "UserAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partners_UserAuths_UserAuthId",
                        column: x => x.UserAuthId,
                        principalTable: "UserAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDirect = table.Column<bool>(type: "bit", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyRegistrationNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RegisteredCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    IndustrySector = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryContactFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactJobTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PrimaryContactPhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TechnicalContactFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TechnicalContactJobTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TechnicalContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    TechnicalContactPhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OperatingPattern = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PrimaryTimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasTechnologyPartner = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_DirectMatchesLogin", "([IsDirect] = 1 AND [UserAuthId] IS NOT NULL) OR ([IsDirect] = 0 AND [UserAuthId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Customers_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_UserAuths_UserAuthId",
                        column: x => x.UserAuthId,
                        principalTable: "UserAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PartnerId",
                table: "Customers",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserAuthId",
                table: "Customers",
                column: "UserAuthId",
                unique: true,
                filter: "[UserAuthId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InternalUsers_UserAuthId",
                table: "InternalUsers",
                column: "UserAuthId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Name",
                table: "Partners",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_UserAuthId",
                table: "Partners",
                column: "UserAuthId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuths_UserType_NormalizedEmail",
                table: "UserAuths",
                columns: new[] { "UserType", "NormalizedEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "InternalUsers");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "UserAuths");
        }
    }
}
