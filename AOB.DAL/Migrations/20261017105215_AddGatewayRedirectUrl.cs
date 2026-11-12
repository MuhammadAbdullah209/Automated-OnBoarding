using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddGatewayRedirectUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                table: "ProjectGatewayConfigurations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                table: "ProjectGatewayConfigurations");
        }
    }
}
