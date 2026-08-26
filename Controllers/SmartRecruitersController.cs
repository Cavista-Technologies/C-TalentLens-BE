using C_TalentLens.Application.Integrations.SmartRecruiters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize(Policy = "RecruitmentWrite")]
[Route("api/integrations/smartrecruiters")]
public class SmartRecruitersController(ISmartRecruitersSyncService smartRecruitersSync) : ControllerBase
{
    [HttpPost("jobs/sync")]
    [ProducesResponseType<SmartRecruitersSyncResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SmartRecruitersSyncResponse>> SyncJobs(CancellationToken cancellationToken)
    {
        var response = await smartRecruitersSync.SyncJobsAsync(cancellationToken);
        return Ok(response);
    }
}
