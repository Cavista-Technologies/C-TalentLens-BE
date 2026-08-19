using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CustomSource = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ActivityDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    HiredAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceActivities_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourceActivities_RequisitionId",
                table: "SourceActivities",
                column: "RequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceActivities");
        }
    }
}
