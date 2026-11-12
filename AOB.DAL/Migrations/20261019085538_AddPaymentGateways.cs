using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGateways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentGateways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGateways", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentGatewayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsInput = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentGatewayFields_PaymentGateways_PaymentGatewayId",
                        column: x => x.PaymentGatewayId,
                        principalTable: "PaymentGateways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayFields_PaymentGatewayId_Name_FieldType_IsInput",
                table: "PaymentGatewayFields",
                columns: new[] { "PaymentGatewayId", "Name", "FieldType", "IsInput" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGateways_Name",
                table: "PaymentGateways",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGatewayFields");

            migrationBuilder.DropTable(
                name: "PaymentGateways");
        }
    }
}
