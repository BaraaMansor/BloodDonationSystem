using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmergencyToBloodRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmergency",
                table: "BloodRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTime(2026, 2, 3, 13, 18, 15, 54, DateTimeKind.Local).AddTicks(5296), "$2a$11$.wYhO6Yd8Pc4IrgIAMLh7e4nmRe18DNdoU.8pk1ajLRfcC6kG.gtS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmergency",
                table: "BloodRequests");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTime(2026, 2, 1, 13, 14, 19, 695, DateTimeKind.Local).AddTicks(6558), "$2a$11$zz7uJBJjCdm/Xv0.hEnwIe0mOz//5ILrPYeVX8ASS6d9S5eKYpA/G" });
        }
    }
}
