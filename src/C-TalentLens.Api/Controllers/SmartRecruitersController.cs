using C_TalentLens.Application.Integrations.SmartRecruiters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize(Policy = "RecruitmentWrite")]
[Route("api/integrations/smartrecruiters")]
public class SmartRecruitersController(ISmartRecruitersSyncService smartRecruitersSync) : ControllerBase
{
    [HttpPost("jobs/sync")]
    [SwaggerOperation(
        Summary = "Sync SmartRecruiters jobs",
        Description = "Synchronizes jobs from the mocked SmartRecruiters integration and creates or updates matching requisitions in C-TalentLens.")]
    [ProducesResponseType<SmartRecruitersSyncResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SmartRecruitersSyncResponse>> SyncJobs(CancellationToken cancellationToken)
    {
        var response = await smartRecruitersSync.SyncJobsAsync(cancellationToken);
        return Ok(response);
    }
}
