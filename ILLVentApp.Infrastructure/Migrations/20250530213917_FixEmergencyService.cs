using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILLVentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEmergencyService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectedByHospitalIds",
                table: "EmergencyRescueRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedByHospitalIds",
                table: "EmergencyRescueRequests");
        }
    }
}
