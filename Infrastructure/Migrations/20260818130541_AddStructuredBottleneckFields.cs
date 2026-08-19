using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredBottleneckFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessImpact",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomCategory",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LessonsLearned",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionOwner",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolutionOwnerUserId",
                table: "Bottleneck",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionSummary",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Bottleneck
                SET Title = Reason,
                    Description = Reason,
                    Category = 'Other',
                    CustomCategory = 'Legacy',
                    Priority = 'Medium',
                    BusinessImpact = 'Impact not specified.'
                WHERE Reason IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Bottleneck");

            migrationBuilder.CreateIndex(
                name: "IX_Bottleneck_ResolutionOwnerUserId",
                table: "Bottleneck",
                column: "ResolutionOwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bottleneck_AspNetUsers_ResolutionOwnerUserId",
                table: "Bottleneck",
                column: "ResolutionOwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bottleneck_AspNetUsers_ResolutionOwnerUserId",
                table: "Bottleneck");

            migrationBuilder.DropIndex(
                name: "IX_Bottleneck_ResolutionOwnerUserId",
                table: "Bottleneck");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Bottleneck",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Bottleneck
                SET Reason = Title
                WHERE Title IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "BusinessImpact",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "CustomCategory",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "LessonsLearned",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "ResolutionOwner",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "ResolutionOwnerUserId",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "ResolutionSummary",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Bottleneck");
        }
    }
}
