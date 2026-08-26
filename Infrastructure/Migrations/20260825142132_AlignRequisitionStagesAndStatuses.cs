using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignRequisitionStagesAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStatus = CASE CurrentStatus
                    WHEN 'Open' THEN 'Active'
                    WHEN 'OnHold' THEN 'Hold'
                    WHEN 'Cancelled' THEN 'Closed'
                    WHEN 'Active' THEN 'Active'
                    WHEN 'Hold' THEN 'Hold'
                    WHEN 'Closed' THEN 'Closed'
                    ELSE 'Active'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStage = CASE CurrentStage
                    WHEN 'Sourcing' THEN 'PipeliningSourcing'
                    WHEN 'Screening' THEN 'SparkHire'
                    WHEN 'Interviewing' THEN 'Interview'
                    WHEN 'OfferStage' THEN 'RequestToHire'
                    WHEN 'OfferExtended' THEN 'OfferedHired'
                    WHEN 'JobPosting' THEN 'JobPosting'
                    WHEN 'PipeliningSourcing' THEN 'PipeliningSourcing'
                    WHEN 'SparkHire' THEN 'SparkHire'
                    WHEN 'Interview' THEN 'Interview'
                    WHEN 'RequestToHire' THEN 'RequestToHire'
                    WHEN 'OfferedHired' THEN 'OfferedHired'
                    ELSE 'JobPosting'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE StageTransition
                SET Status = CASE Status
                    WHEN 'Sourcing' THEN 'PipeliningSourcing'
                    WHEN 'Screening' THEN 'SparkHire'
                    WHEN 'Interviewing' THEN 'Interview'
                    WHEN 'OfferStage' THEN 'RequestToHire'
                    WHEN 'OfferExtended' THEN 'OfferedHired'
                    WHEN 'JobPosting' THEN 'JobPosting'
                    WHEN 'PipeliningSourcing' THEN 'PipeliningSourcing'
                    WHEN 'SparkHire' THEN 'SparkHire'
                    WHEN 'Interview' THEN 'Interview'
                    WHEN 'RequestToHire' THEN 'RequestToHire'
                    WHEN 'OfferedHired' THEN 'OfferedHired'
                    ELSE 'JobPosting'
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStatus = CASE CurrentStatus
                    WHEN 'Active' THEN 'Open'
                    WHEN 'Hold' THEN 'OnHold'
                    WHEN 'Closed' THEN 'Closed'
                    ELSE 'Open'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET CurrentStage = CASE CurrentStage
                    WHEN 'JobPosting' THEN 'Sourcing'
                    WHEN 'PipeliningSourcing' THEN 'Sourcing'
                    WHEN 'SparkHire' THEN 'Screening'
                    WHEN 'Interview' THEN 'Interviewing'
                    WHEN 'RequestToHire' THEN 'OfferStage'
                    WHEN 'OfferedHired' THEN 'OfferExtended'
                    ELSE 'Sourcing'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE StageTransition
                SET Status = CASE Status
                    WHEN 'JobPosting' THEN 'Sourcing'
                    WHEN 'PipeliningSourcing' THEN 'Sourcing'
                    WHEN 'SparkHire' THEN 'Screening'
                    WHEN 'Interview' THEN 'Interviewing'
                    WHEN 'RequestToHire' THEN 'OfferStage'
                    WHEN 'OfferedHired' THEN 'OfferExtended'
                    ELSE 'Sourcing'
                END
                """);
        }
    }
}
