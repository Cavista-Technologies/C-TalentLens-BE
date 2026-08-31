using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkRequisitionOwnershipToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HiringManagerUserId",
                table: "Requisitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecruiterUserId",
                table: "Requisitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Bottleneck",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "ActionItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET HiringManagerUserId = COALESCE(
                    (SELECT Id FROM AspNetUsers WHERE AspNetUsers.FullName = Requisitions.HiringManager LIMIT 1),
                    (SELECT Id FROM AspNetUsers LIMIT 1));
                """);

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET RecruiterUserId = COALESCE(
                    (SELECT Id FROM AspNetUsers WHERE AspNetUsers.FullName = Requisitions.Recruiter LIMIT 1),
                    (SELECT Id FROM AspNetUsers LIMIT 1));
                """);

            migrationBuilder.Sql("""
                UPDATE Bottleneck
                SET OwnerUserId = COALESCE(
                    (SELECT Id FROM AspNetUsers WHERE AspNetUsers.FullName = Bottleneck.Owner LIMIT 1),
                    (SELECT Id FROM AspNetUsers LIMIT 1));
                """);

            migrationBuilder.Sql("""
                UPDATE ActionItem
                SET OwnerUserId = COALESCE(
                    (SELECT Id FROM AspNetUsers WHERE AspNetUsers.FullName = ActionItem.Owner LIMIT 1),
                    (SELECT Id FROM AspNetUsers LIMIT 1));
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "HiringManagerUserId",
                table: "Requisitions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RecruiterUserId",
                table: "Requisitions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "Bottleneck",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "ActionItem",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_HiringManagerUserId",
                table: "Requisitions",
                column: "HiringManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_RecruiterUserId",
                table: "Requisitions",
                column: "RecruiterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bottleneck_OwnerUserId",
                table: "Bottleneck",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItem_OwnerUserId",
                table: "ActionItem",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItem_AspNetUsers_OwnerUserId",
                table: "ActionItem",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bottleneck_AspNetUsers_OwnerUserId",
                table: "Bottleneck",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_HiringManagerUserId",
                table: "Requisitions",
                column: "HiringManagerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_RecruiterUserId",
                table: "Requisitions",
                column: "RecruiterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItem_AspNetUsers_OwnerUserId",
                table: "ActionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Bottleneck_AspNetUsers_OwnerUserId",
                table: "Bottleneck");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_HiringManagerUserId",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_RecruiterUserId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_HiringManagerUserId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_RecruiterUserId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Bottleneck_OwnerUserId",
                table: "Bottleneck");

            migrationBuilder.DropIndex(
                name: "IX_ActionItem_OwnerUserId",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "HiringManagerUserId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "RecruiterUserId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Bottleneck");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "ActionItem");
        }
    }
}
