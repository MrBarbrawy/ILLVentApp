using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILLVentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmergencyRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequestStatus",
                table: "EmergencyRescueRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "HospitalResponseTime",
                table: "EmergencyRescueRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationTimestamp",
                table: "EmergencyRescueRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistorySnapshot",
                table: "EmergencyRescueRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistorySource",
                table: "EmergencyRescueRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotifiedHospitalIds",
                table: "EmergencyRescueRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RequestPriority",
                table: "EmergencyRescueRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<double>(
                name: "UserLatitude",
                table: "EmergencyRescueRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "UserLongitude",
                table: "EmergencyRescueRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HospitalResponseTime",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "LocationTimestamp",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "MedicalHistorySnapshot",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "MedicalHistorySource",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "NotifiedHospitalIds",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "RequestPriority",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "UserLatitude",
                table: "EmergencyRescueRequests");

            migrationBuilder.DropColumn(
                name: "UserLongitude",
                table: "EmergencyRescueRequests");

            migrationBuilder.AlterColumn<string>(
                name: "RequestStatus",
                table: "EmergencyRescueRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
