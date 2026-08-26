using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitRequisitionStatusAndStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentStage",
                table: "Requisitions",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Sourcing");

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStage = CASE CurrentStatus
                    WHEN 'Sourcing' THEN 'Sourcing'
                    WHEN 'Screening' THEN 'Screening'
                    WHEN 'Interviewing' THEN 'Interviewing'
                    WHEN 'OfferStage' THEN 'OfferStage'
                    WHEN 'OfferExtended' THEN 'OfferExtended'
                    WHEN 'Closed' THEN 'OfferExtended'
                    ELSE 'Sourcing'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStatus = CASE
                    WHEN CurrentStatus IN ('Closed', 'Cancelled') THEN CurrentStatus
                    ELSE 'Open'
                END
                """);

            migrationBuilder.DropColumn(
                name: "AdvertisementDate",
                table: "Requisitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStage",
                table: "Requisitions");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AdvertisementDate",
                table: "Requisitions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET AdvertisementDate = DateOpened
                """);
        }
    }
}
