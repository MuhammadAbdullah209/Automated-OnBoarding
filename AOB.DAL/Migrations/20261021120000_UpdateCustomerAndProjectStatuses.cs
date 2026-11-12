using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerAndProjectStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Customers SET Status = N'Onboarded' WHERE Status = N'OnboardingCompleted';
                UPDATE Customers SET Status = N'Pending' WHERE Status IN (N'Approval', N'ChangesRequested');
                """);

            migrationBuilder.Sql("""
                UPDATE Projects SET Status = N'InProgress' WHERE Status = N'Draft';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Customers SET Status = N'OnboardingCompleted' WHERE Status = N'Onboarded';
                UPDATE Projects SET Status = N'Draft' WHERE Status = N'InProgress' AND CurrentStep = N'Discovery';
                """);
        }
    }
}
