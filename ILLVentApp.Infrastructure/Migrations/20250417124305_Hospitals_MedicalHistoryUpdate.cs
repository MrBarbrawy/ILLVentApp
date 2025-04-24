using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILLVentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Hospitals_MedicalHistoryUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImmunizationHistories_AspNetUsers_UserId",
                table: "ImmunizationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SocialHistories_AspNetUsers_UserId",
                table: "SocialHistories");

            migrationBuilder.DropTable(
                name: "FamilyMedicalHistories");

            migrationBuilder.DropIndex(
                name: "IX_SocialHistories_UserId",
                table: "SocialHistories");

            migrationBuilder.DropIndex(
                name: "IX_ImmunizationHistories_UserId",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "AlcoholDrinksPerDay",
                table: "SocialHistories");

            migrationBuilder.DropColumn(
                name: "SmokingStatus",
                table: "SocialHistories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SocialHistories");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "AspirinUsage",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "BloodTransfusionObjection",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "Conditions",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "SurName",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "SurgeryHistory",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "DateAdministered",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "VaccineType",
                table: "ImmunizationHistories");

            migrationBuilder.RenameColumn(
                name: "AlcoholDrinksPerWeek",
                table: "SocialHistories",
                newName: "MedicalHistoryId");

            migrationBuilder.RenameColumn(
                name: "SocialHistoryId",
                table: "SocialHistories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ImmunizationId",
                table: "ImmunizationHistories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Hospitals",
                newName: "ContactNumber");

            migrationBuilder.AlterColumn<int>(
                name: "YearsSmoked",
                table: "SocialHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "YearStopped",
                table: "SocialHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PacksPerDay",
                table: "SocialHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseType",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseFrequency",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "HasLowBloodPressure",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "HasHighBloodPressure",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "HasDiabetes",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "BirthControlMethod",
                table: "MedicalHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AllergiesDetails",
                table: "MedicalHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasAllergies",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBloodTransfusionObjection",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSurgeryHistory",
                table: "MedicalHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "MedicalHistories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeExpiresAt",
                table: "MedicalHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeGeneratedAt",
                table: "MedicalHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FluDate",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasFlu",
                table: "ImmunizationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasHepA",
                table: "ImmunizationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasHepB",
                table: "ImmunizationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPneumonia",
                table: "ImmunizationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTetanus",
                table: "ImmunizationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HepADate",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HepBDate",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MedicalHistoryId",
                table: "ImmunizationHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PneumoniaDate",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TetanusDate",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Hospitals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Established",
                table: "Hospitals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Hospitals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Hospitals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Hospitals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Hospitals",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Specialties",
                table: "Hospitals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "Hospitals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FamilyHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalHistoryId = table.Column<int>(type: "int", nullable: false),
                    HasCancerPolyps = table.Column<bool>(type: "bit", nullable: false),
                    CancerPolypsRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasAnemia = table.Column<bool>(type: "bit", nullable: false),
                    AnemiaRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasDiabetes = table.Column<bool>(type: "bit", nullable: false),
                    DiabetesRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasBloodClots = table.Column<bool>(type: "bit", nullable: false),
                    BloodClotsRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasHeartDisease = table.Column<bool>(type: "bit", nullable: false),
                    HeartDiseaseRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasStroke = table.Column<bool>(type: "bit", nullable: false),
                    StrokeRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasHighBloodPressure = table.Column<bool>(type: "bit", nullable: false),
                    HighBloodPressureRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasAnesthesiaReaction = table.Column<bool>(type: "bit", nullable: false),
                    AnesthesiaReactionRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasBleedingProblems = table.Column<bool>(type: "bit", nullable: false),
                    BleedingProblemsRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasHepatitis = table.Column<bool>(type: "bit", nullable: false),
                    HepatitisRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasOtherCondition = table.Column<bool>(type: "bit", nullable: false),
                    OtherConditionDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherConditionRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyHistories_MedicalHistories_MedicalHistoryId",
                        column: x => x.MedicalHistoryId,
                        principalTable: "MedicalHistories",
                        principalColumn: "MedicalHistoryId");
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalHistoryId = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalConditions_MedicalHistories_MedicalHistoryId",
                        column: x => x.MedicalHistoryId,
                        principalTable: "MedicalHistories",
                        principalColumn: "MedicalHistoryId");
                });

            migrationBuilder.CreateTable(
                name: "SurgicalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalHistoryId = table.Column<int>(type: "int", nullable: false),
                    SurgeryType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurgicalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurgicalHistories_MedicalHistories_MedicalHistoryId",
                        column: x => x.MedicalHistoryId,
                        principalTable: "MedicalHistories",
                        principalColumn: "MedicalHistoryId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialHistories_MedicalHistoryId",
                table: "SocialHistories",
                column: "MedicalHistoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImmunizationHistories_MedicalHistoryId",
                table: "ImmunizationHistories",
                column: "MedicalHistoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyHistories_MedicalHistoryId",
                table: "FamilyHistories",
                column: "MedicalHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_MedicalHistoryId",
                table: "MedicalConditions",
                column: "MedicalHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SurgicalHistories_MedicalHistoryId",
                table: "SurgicalHistories",
                column: "MedicalHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImmunizationHistories_MedicalHistories_MedicalHistoryId",
                table: "ImmunizationHistories",
                column: "MedicalHistoryId",
                principalTable: "MedicalHistories",
                principalColumn: "MedicalHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SocialHistories_MedicalHistories_MedicalHistoryId",
                table: "SocialHistories",
                column: "MedicalHistoryId",
                principalTable: "MedicalHistories",
                principalColumn: "MedicalHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImmunizationHistories_MedicalHistories_MedicalHistoryId",
                table: "ImmunizationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SocialHistories_MedicalHistories_MedicalHistoryId",
                table: "SocialHistories");

            migrationBuilder.DropTable(
                name: "FamilyHistories");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "SurgicalHistories");

            migrationBuilder.DropIndex(
                name: "IX_SocialHistories_MedicalHistoryId",
                table: "SocialHistories");

            migrationBuilder.DropIndex(
                name: "IX_ImmunizationHistories_MedicalHistoryId",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "AllergiesDetails",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "HasAllergies",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "HasBloodTransfusionObjection",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "HasSurgeryHistory",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "QrCodeExpiresAt",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "QrCodeGeneratedAt",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "FluDate",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HasFlu",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HasHepA",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HasHepB",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HasPneumonia",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HasTetanus",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HepADate",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "HepBDate",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "MedicalHistoryId",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "PneumoniaDate",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "TetanusDate",
                table: "ImmunizationHistories");

            migrationBuilder.DropColumn(
                name: "Established",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Specialties",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Hospitals");

            migrationBuilder.RenameColumn(
                name: "MedicalHistoryId",
                table: "SocialHistories",
                newName: "AlcoholDrinksPerWeek");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SocialHistories",
                newName: "SocialHistoryId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ImmunizationHistories",
                newName: "ImmunizationId");

            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Hospitals",
                newName: "Phone");

            migrationBuilder.AlterColumn<int>(
                name: "YearsSmoked",
                table: "SocialHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "YearStopped",
                table: "SocialHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "PacksPerDay",
                table: "SocialHistories",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseType",
                table: "SocialHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseFrequency",
                table: "SocialHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "AlcoholDrinksPerDay",
                table: "SocialHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SmokingStatus",
                table: "SocialHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SocialHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "HasLowBloodPressure",
                table: "MedicalHistories",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "HasHighBloodPressure",
                table: "MedicalHistories",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "HasDiabetes",
                table: "MedicalHistories",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "BirthControlMethod",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AspirinUsage",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BloodTransfusionObjection",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Conditions",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "MedicalHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SurName",
                table: "MedicalHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SurgeryHistory",
                table: "MedicalHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAdministered",
                table: "ImmunizationHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ImmunizationHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VaccineType",
                table: "ImmunizationHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Hospitals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "FamilyMedicalHistories",
                columns: table => new
                {
                    FamilyHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMedicalHistories", x => x.FamilyHistoryId);
                    table.ForeignKey(
                        name: "FK_FamilyMedicalHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialHistories_UserId",
                table: "SocialHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImmunizationHistories_UserId",
                table: "ImmunizationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMedicalHistories_UserId",
                table: "FamilyMedicalHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImmunizationHistories_AspNetUsers_UserId",
                table: "ImmunizationHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SocialHistories_AspNetUsers_UserId",
                table: "SocialHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
