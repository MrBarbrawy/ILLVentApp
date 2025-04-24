using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILLVentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Pharmacies_Edit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Pharmacies");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Pharmacies",
                newName: "ContactNumber");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Pharmacies",
                newName: "Rating");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Pharmacies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "AcceptPrivateInsurance",
                table: "Pharmacies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Pharmacies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Pharmacies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "Pharmacies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptPrivateInsurance",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Pharmacies");

            migrationBuilder.RenameColumn(
                name: "Rating",
                table: "Pharmacies",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Pharmacies",
                newName: "Phone");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Pharmacies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Pharmacies",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
