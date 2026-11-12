using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Services = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CrmSystem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CrmPlatform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDiscoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentPaymentMethods = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PaymentJourneyDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CrmPaymentExperience = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDiscoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDiscoveries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectGatewayConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostTransactionMethods = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactCentrePlatform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ConnectionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    HandoverNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectGatewayConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectGatewayConfigurations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectGatewayFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsInput = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectGatewayFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectGatewayFields_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPaymentConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SupportedCards = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxPaymentAttempts = table.Column<int>(type: "int", nullable: true),
                    TotalMerchantAccounts = table.Column<int>(type: "int", nullable: true),
                    CustomerAcquirer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Currencies = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CardNumberMasking = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SmsServiceProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OtherSmsProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    EmailServiceProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OtherEmailProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HasSandboxEnvironment = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPaymentConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPaymentConfigurations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTechnicalEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentRegions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StagingRegions = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProductionRegions = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OutboundAccess = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTechnicalEnvironments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTechnicalEnvironments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDiscoveries_ProjectId",
                table: "ProjectDiscoveries",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId",
                table: "ProjectDocuments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGatewayConfigurations_ProjectId",
                table: "ProjectGatewayConfigurations",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGatewayFields_ProjectId_Field_FieldType_IsInput",
                table: "ProjectGatewayFields",
                columns: new[] { "ProjectId", "Field", "FieldType", "IsInput" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPaymentConfigurations_ProjectId",
                table: "ProjectPaymentConfigurations",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId",
                table: "Projects",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnicalEnvironments_ProjectId",
                table: "ProjectTechnicalEnvironments",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDiscoveries");

            migrationBuilder.DropTable(
                name: "ProjectDocuments");

            migrationBuilder.DropTable(
                name: "ProjectGatewayConfigurations");

            migrationBuilder.DropTable(
                name: "ProjectGatewayFields");

            migrationBuilder.DropTable(
                name: "ProjectPaymentConfigurations");

            migrationBuilder.DropTable(
                name: "ProjectTechnicalEnvironments");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
