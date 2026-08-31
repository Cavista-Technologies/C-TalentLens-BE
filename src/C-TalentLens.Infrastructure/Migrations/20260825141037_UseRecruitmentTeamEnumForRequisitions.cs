using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_TalentLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseRecruitmentTeamEnumForRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET Department = CASE Department
                    WHEN 'Talent' THEN 'Talent'
                    WHEN 'Engineering' THEN 'Engineering'
                    WHEN 'Product' THEN 'Product'
                    WHEN 'Client Experience' THEN 'ClientExperience'
                    WHEN 'ClientExperience' THEN 'ClientExperience'
                    WHEN 'People' THEN 'People'
                    WHEN 'People Analytics' THEN 'People'
                    WHEN 'IT/Infrastructure' THEN 'ITInfrastructure'
                    WHEN 'ITInfrastructure' THEN 'ITInfrastructure'
                    WHEN 'Operations' THEN 'Operations'
                    WHEN 'Creative' THEN 'Creative'
                    WHEN 'Marketing and Communications' THEN 'MarketingAndCommunications'
                    WHEN 'MarketingAndCommunications' THEN 'MarketingAndCommunications'
                    WHEN 'Sales' THEN 'Sales'
                    ELSE 'Talent'
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Requisitions
                SET Department = CASE Department
                    WHEN 'ClientExperience' THEN 'Client Experience'
                    WHEN 'ITInfrastructure' THEN 'IT/Infrastructure'
                    WHEN 'MarketingAndCommunications' THEN 'Marketing and Communications'
                    ELSE Department
                END
                """);
        }
    }
}
