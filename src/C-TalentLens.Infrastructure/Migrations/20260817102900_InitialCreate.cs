using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    HiringManager = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Recruiter = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DateOpened = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AdvertisementDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HiringGoal = table.Column<int>(type: "INTEGER", nullable: false),
                    FilledGoal = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OfferExtendedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ClosedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItem_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bottleneck",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bottleneck", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bottleneck_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageTransition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EnteredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExitedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageTransition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageTransition_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItem_RequisitionId",
                table: "ActionItem",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bottleneck_RequisitionId",
                table: "Bottleneck",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_RequisitionCode",
                table: "Requisitions",
                column: "RequisitionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StageTransition_RequisitionId",
                table: "StageTransition",
                column: "RequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionItem");

            migrationBuilder.DropTable(
                name: "Bottleneck");

            migrationBuilder.DropTable(
                name: "StageTransition");

            migrationBuilder.DropTable(
                name: "Requisitions");
        }
    }
}
