using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize(Policy = "RecruitmentWrite")]
[Route("api/imports")]
public class ImportsController(IImportService imports) : ControllerBase
{
    [HttpPost("referrals")]
    [SwaggerOperation(
        Summary = "Import referrals",
        Description = "Imports referral rows parsed by the client from an approved CSV template and returns row-level success or validation results.")]
    [ProducesResponseType<ImportResultResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportResultResponse>> ImportReferrals(
        IReadOnlyCollection<ReferralImportRowRequest> rows,
        CancellationToken cancellationToken)
    {
        var response = await imports.ImportReferralsAsync(
            AccessScope.FromPrincipal(User),
            rows,
            cancellationToken);
        return Ok(response);
    }

    [HttpPost("requisitions")]
    [SwaggerOperation(
        Summary = "Import requisitions",
        Description = "Imports requisition rows parsed by the client from an approved CSV template and returns row-level success or validation results.")]
    [ProducesResponseType<ImportResultResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportResultResponse>> ImportRequisitions(
        IReadOnlyCollection<RequisitionImportRowRequest> rows,
        CancellationToken cancellationToken)
    {
        var response = await imports.ImportRequisitionsAsync(
            AccessScope.FromPrincipal(User),
            rows,
            cancellationToken);
        return Ok(response);
    }
}
