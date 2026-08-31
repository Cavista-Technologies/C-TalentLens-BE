using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingAndScopeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StageTransition_RequisitionId",
                table: "StageTransition");

            migrationBuilder.CreateIndex(
                name: "IX_StageTransition_RequisitionId_EnteredAt",
                table: "StageTransition",
                columns: new[] { "RequisitionId", "EnteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceActivities_ActivityDate_Status",
                table: "SourceActivities",
                columns: new[] { "ActivityDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceActivities_HiredAt",
                table: "SourceActivities",
                column: "HiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ClosedDate",
                table: "Requisitions",
                column: "ClosedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_CurrentStatus_CurrentStage",
                table: "Requisitions",
                columns: new[] { "CurrentStatus", "CurrentStage" });

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_DateOpened",
                table: "Requisitions",
                column: "DateOpened");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_Department_Priority",
                table: "Requisitions",
                columns: new[] { "Department", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerDepartment",
                table: "Referrals",
                column: "ReferrerDepartment");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status_HiringOutcome",
                table: "Referrals",
                columns: new[] { "Status", "HiringOutcome" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_SubmissionDate",
                table: "Referrals",
                column: "SubmissionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bottleneck_CreatedAt",
                table: "Bottleneck",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bottleneck_Status_Priority",
                table: "Bottleneck",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItem_DueDate",
                table: "ActionItem",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItem_Status_Priority",
                table: "ActionItem",
                columns: new[] { "Status", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StageTransition_RequisitionId_EnteredAt",
                table: "StageTransition");

            migrationBuilder.DropIndex(
                name: "IX_SourceActivities_ActivityDate_Status",
                table: "SourceActivities");

            migrationBuilder.DropIndex(
                name: "IX_SourceActivities_HiredAt",
                table: "SourceActivities");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_ClosedDate",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_CurrentStatus_CurrentStage",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_DateOpened",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_Department_Priority",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_ReferrerDepartment",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_Status_HiringOutcome",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_SubmissionDate",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Bottleneck_CreatedAt",
                table: "Bottleneck");

            migrationBuilder.DropIndex(
                name: "IX_Bottleneck_Status_Priority",
                table: "Bottleneck");

            migrationBuilder.DropIndex(
                name: "IX_ActionItem_DueDate",
                table: "ActionItem");

            migrationBuilder.DropIndex(
                name: "IX_ActionItem_Status_Priority",
                table: "ActionItem");

            migrationBuilder.CreateIndex(
                name: "IX_StageTransition_RequisitionId",
                table: "StageTransition",
                column: "RequisitionId");
        }
    }
}
