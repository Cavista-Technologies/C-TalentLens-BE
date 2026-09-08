using C_TalentLens.Application.Dtos;

namespace C_TalentLens.Application;

public interface IUserDirectoryService
{
    Task<PagedResponse<UserSummaryResponse>> ListAsync(
        UserDirectoryQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken);
}
