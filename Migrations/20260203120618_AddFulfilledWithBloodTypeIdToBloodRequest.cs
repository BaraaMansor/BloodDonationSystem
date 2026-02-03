using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfilledWithBloodTypeIdToBloodRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FulfilledWithBloodTypeId",
                table: "BloodRequests",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTime(2026, 2, 3, 15, 6, 18, 130, DateTimeKind.Local).AddTicks(1882), "$2a$11$ZE7L31d/SKIA4rqEBGooneFmDmJVvHf4G74nmsUIaLh97NJfTkIL6" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfilledWithBloodTypeId",
                table: "BloodRequests");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTime(2026, 2, 3, 13, 18, 15, 54, DateTimeKind.Local).AddTicks(5296), "$2a$11$.wYhO6Yd8Pc4IrgIAMLh7e4nmRe18DNdoU.8pk1ajLRfcC6kG.gtS" });
        }
    }
}
