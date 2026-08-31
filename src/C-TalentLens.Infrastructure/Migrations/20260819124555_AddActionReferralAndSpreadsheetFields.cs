using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActionReferralAndSpreadsheetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomOpeningReason",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiringManagerNotes",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningReason",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostingType",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusComment",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompletedBy",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByUserId",
                table: "ActionItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCategory",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ActionItem",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ActionItemHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FromValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ToValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItemHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItemHistory_ActionItem_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "ActionItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionItemHistory_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferrerName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ReferrerEmployeeId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ReferrerDepartment = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CandidateName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CandidateEmail = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    CandidatePhoneNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ResumeUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubmitterEmail = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    SubmitterName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    FormStartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FormCompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CandidateRelationship = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CandidateKnownDuration = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CandidateAlignmentComment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubmissionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    HiringOutcome = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    HiredAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferralHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferralId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FromValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ToValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralHistory_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferralHistory_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItem_CompletedByUserId",
                table: "ActionItem",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItemHistory_ActionItemId",
                table: "ActionItemHistory",
                column: "ActionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItemHistory_ChangedByUserId",
                table: "ActionItemHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralHistory_ChangedByUserId",
                table: "ReferralHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralHistory_ReferralId",
                table: "ReferralHistory",
                column: "ReferralId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_RequisitionId",
                table: "Referrals",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItem_AspNetUsers_CompletedByUserId",
                table: "ActionItem",
                column: "CompletedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItem_AspNetUsers_CompletedByUserId",
                table: "ActionItem");

            migrationBuilder.DropTable(
                name: "ActionItemHistory");

            migrationBuilder.DropTable(
                name: "ReferralHistory");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_ActionItem_CompletedByUserId",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "CustomOpeningReason",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "HiringManagerNotes",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "OpeningReason",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "PostingType",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "StatusComment",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "CustomCategory",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ActionItem");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ActionItem");
        }
    }
}
