using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize(Policy = "RecruitmentWrite")]
[Route("api/imports")]
public class ImportsController(IImportService imports) : ControllerBase
{
    [HttpPost("referrals")]
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
