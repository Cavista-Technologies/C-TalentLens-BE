using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure.Services;

public class UserDirectoryService(TalentLensDbContext dbContext) : IUserDirectoryService
{
    public async Task<PagedResponse<UserSummaryResponse>> ListAsync(
        UserDirectoryQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var users = ApplyQuery(dbContext.Users.AsNoTracking(), query)
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email);

        var totalItems = await users.CountAsync(cancellationToken);
        var pageUsers = await users
            .Skip((pageRequest.NormalizedPage - 1) * pageRequest.NormalizedPageSize)
            .Take(pageRequest.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var rolesByUserId = await LoadRolesByUserIdAsync(
            pageUsers.Select(user => user.Id).ToList(),
            cancellationToken);

        var items = pageUsers
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.Department,
                user.ReportingLine,
                rolesByUserId.GetValueOrDefault(user.Id, [])))
            .ToList();

        return Pagination.ToPagedResponse(
            items,
            pageRequest,
            "Users retrieved successfully.",
            totalItems);
    }

    private IQueryable<ApplicationUser> ApplyQuery(IQueryable<ApplicationUser> users, UserDirectoryQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = query.Role.Trim().ToUpperInvariant();
            users = users.Where(user => dbContext.UserRoles
                .Join(
                    dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.NormalizedName })
                .Any(role => role.UserId == user.Id && role.NormalizedName == normalizedRole));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(user =>
                EF.Functions.Like(user.FullName, term) ||
                (user.Email != null && EF.Functions.Like(user.Email, term)) ||
                (user.Department != null && EF.Functions.Like(user.Department, term)) ||
                (user.ReportingLine != null && EF.Functions.Like(user.ReportingLine, term)) ||
                dbContext.UserRoles
                    .Join(
                        dbContext.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => new { userRole.UserId, role.Name })
                    .Any(role => role.UserId == user.Id &&
                                 role.Name != null &&
                                 EF.Functions.Like(role.Name, term)));
        }

        return users;
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> LoadRolesByUserIdAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<string>>();
        }

        var roles = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                RoleName = role.Name ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return roles
            .GroupBy(role => role.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(role => role.RoleName)
                    .OrderBy(role => role)
                    .ToList());
    }
}
