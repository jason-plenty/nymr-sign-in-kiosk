using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NymrSignIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveToKioskSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kiosk");

            migrationBuilder.RenameTable(
                name: "SiteRegisterEntries",
                newName: "SiteRegisterEntries",
                newSchema: "kiosk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SiteRegisterEntries",
                schema: "kiosk",
                newName: "SiteRegisterEntries");
        }
    }
}
