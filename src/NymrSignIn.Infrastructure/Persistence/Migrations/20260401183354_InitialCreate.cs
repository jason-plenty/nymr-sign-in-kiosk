using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NymrSignIn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kiosk");

            migrationBuilder.CreateTable(
                name: "SiteRegisterEntries",
                schema: "kiosk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Organisation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignatureUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DateIn = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeIn = table.Column<TimeOnly>(type: "time", nullable: false),
                    TimeOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteRegisterEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteRegisterEntries_DateIn",
                schema: "kiosk",
                table: "SiteRegisterEntries",
                column: "DateIn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteRegisterEntries",
                schema: "kiosk");
        }
    }
}
