using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameInternalRoleToSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The role "Internal" (the admin) is now called "SuperAdmin". UserType is stored as text.
            migrationBuilder.Sql("UPDATE [UserAuths] SET [UserType] = 'SuperAdmin' WHERE [UserType] = 'Internal';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [UserAuths] SET [UserType] = 'Internal' WHERE [UserType] = 'SuperAdmin';");
        }
    }
}
