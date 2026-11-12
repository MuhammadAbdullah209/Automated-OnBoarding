using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildStandardFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildReasons",
                table: "Projects",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildType",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStandard",
                table: "PaymentGateways",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStandard",
                table: "PaymentGatewayFields",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildReasons",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BuildType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsStandard",
                table: "PaymentGateways");

            migrationBuilder.DropColumn(
                name: "IsStandard",
                table: "PaymentGatewayFields");
        }
    }
}
