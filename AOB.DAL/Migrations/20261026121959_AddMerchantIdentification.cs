using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantIdentification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantIdentification",
                table: "ProjectPaymentConfigurations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantIdentificationOther",
                table: "ProjectPaymentConfigurations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantIdentification",
                table: "ProjectPaymentConfigurations");

            migrationBuilder.DropColumn(
                name: "MerchantIdentificationOther",
                table: "ProjectPaymentConfigurations");
        }
    }
}
