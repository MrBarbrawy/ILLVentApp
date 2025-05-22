using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILLVentApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMedicalHistoryEntityRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure YearStopped in SocialHistories is nullable and proper type
            migrationBuilder.AlterColumn<int>(
                name: "YearStopped",
                table: "SocialHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Ensure ExerciseType and ExerciseFrequency can be empty
            migrationBuilder.AlterColumn<string>(
                name: "ExerciseType",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseFrequency",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // Make sure we handle the cascade delete between MedicalHistory and child entities
            migrationBuilder.Sql(@"
                ALTER TABLE [SocialHistories] DROP CONSTRAINT [FK_SocialHistories_MedicalHistories_MedicalHistoryId];
                ALTER TABLE [SocialHistories] ADD CONSTRAINT [FK_SocialHistories_MedicalHistories_MedicalHistoryId] 
                FOREIGN KEY ([MedicalHistoryId]) REFERENCES [MedicalHistories] ([MedicalHistoryId]) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "ExerciseType",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseFrequency",
                table: "SocialHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Revert cascade delete change
            migrationBuilder.Sql(@"
                ALTER TABLE [SocialHistories] DROP CONSTRAINT [FK_SocialHistories_MedicalHistories_MedicalHistoryId];
                ALTER TABLE [SocialHistories] ADD CONSTRAINT [FK_SocialHistories_MedicalHistories_MedicalHistoryId] 
                FOREIGN KEY ([MedicalHistoryId]) REFERENCES [MedicalHistories] ([MedicalHistoryId]);
            ");
        }
    }
}
