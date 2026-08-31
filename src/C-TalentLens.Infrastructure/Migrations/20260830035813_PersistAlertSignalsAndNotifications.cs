using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistAlertSignalsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StableKey = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ActionLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastDetectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertSignals_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertSignalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RecipientRole = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DismissedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AlertSignals_AlertSignalId",
                        column: x => x.AlertSignalId,
                        principalTable: "AlertSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertSignals_RequisitionId",
                table: "AlertSignals",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSignals_StableKey",
                table: "AlertSignals",
                column: "StableKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertSignals_Status_RequisitionCode",
                table: "AlertSignals",
                columns: new[] { "Status", "RequisitionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertSignals_Status_Severity_Type",
                table: "AlertSignals",
                columns: new[] { "Status", "Severity", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AlertSignalId_RecipientUserId",
                table: "Notifications",
                columns: new[] { "AlertSignalId", "RecipientUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientRole_Status",
                table: "Notifications",
                columns: new[] { "RecipientRole", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_Status",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "AlertSignals");
        }
    }
}
