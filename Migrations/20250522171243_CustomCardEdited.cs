using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Migrations
{
    /// <inheritdoc />
    public partial class CustomCardEdited : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Answer",
                table: "CustomCards");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CustomCards");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                table: "CustomCards");

            migrationBuilder.DropColumn(
                name: "NextRepetitionDate",
                table: "CustomCards");

            migrationBuilder.DropColumn(
                name: "RepetitionLevel",
                table: "CustomCards");

            migrationBuilder.RenameColumn(
                name: "Question",
                table: "CustomCards",
                newName: "QuestionText");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "CustomCards",
                newName: "AnswerText");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionId",
                table: "SavedQuestions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SavedQuestions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomCardId",
                table: "SavedQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EF",
                table: "SavedQuestions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "SavedQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReview",
                table: "SavedQuestions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Repetition",
                table: "SavedQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedQuestions_CustomCardId",
                table: "SavedQuestions",
                column: "CustomCardId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedQuestions_QuestionId",
                table: "SavedQuestions",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedQuestions_CustomCards_CustomCardId",
                table: "SavedQuestions",
                column: "CustomCardId",
                principalTable: "CustomCards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedQuestions_Questions_QuestionId",
                table: "SavedQuestions",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedQuestions_CustomCards_CustomCardId",
                table: "SavedQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedQuestions_Questions_QuestionId",
                table: "SavedQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SavedQuestions_CustomCardId",
                table: "SavedQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SavedQuestions_QuestionId",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "CustomCardId",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "EF",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "NextReview",
                table: "SavedQuestions");

            migrationBuilder.DropColumn(
                name: "Repetition",
                table: "SavedQuestions");

            migrationBuilder.RenameColumn(
                name: "QuestionText",
                table: "CustomCards",
                newName: "Question");

            migrationBuilder.RenameColumn(
                name: "AnswerText",
                table: "CustomCards",
                newName: "Category");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionId",
                table: "SavedQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Answer",
                table: "CustomCards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CustomCards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedAt",
                table: "CustomCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRepetitionDate",
                table: "CustomCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepetitionLevel",
                table: "CustomCards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
