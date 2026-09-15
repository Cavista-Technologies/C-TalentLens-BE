using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceActivityReferralLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferralId",
                table: "SourceActivities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceActivities_ReferralId",
                table: "SourceActivities",
                column: "ReferralId",
                unique: true,
                filter: "\"ReferralId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourceActivities_ReferralId",
                table: "SourceActivities");

            migrationBuilder.DropColumn(
                name: "ReferralId",
                table: "SourceActivities");
        }
    }
}
