using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NymrSignIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalDeclarationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalStatus",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NotDeclared");

            migrationBuilder.AddColumn<string>(
                name: "SiteCode",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteCodeGenerated",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "OnSite");

            migrationBuilder.Sql(
                "UPDATE kiosk.SiteRegisterEntries SET Status = 'OnSite', MedicalStatus = 'Fit' " +
                "WHERE Status IS NULL OR Status = '';");

            migrationBuilder.CreateIndex(
                name: "IX_SiteRegisterEntries_DateIn_Status",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                columns: new[] { "DateIn", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteRegisterEntries_DateIn_Status",
                schema: "kiosk",
                table: "SiteRegisterEntries");

            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                schema: "kiosk",
                table: "SiteRegisterEntries");

            migrationBuilder.DropColumn(
                name: "MedicalStatus",
                schema: "kiosk",
                table: "SiteRegisterEntries");

            migrationBuilder.DropColumn(
                name: "SiteCode",
                schema: "kiosk",
                table: "SiteRegisterEntries");

            migrationBuilder.DropColumn(
                name: "SiteCodeGenerated",
                schema: "kiosk",
                table: "SiteRegisterEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "kiosk",
                table: "SiteRegisterEntries");
        }
    }
}
