namespace C_TalentLens.Application.Dtos;

public record HiringTrendResponse(
    IReadOnlyCollection<MonthlyHiringTrendResponse> MonthlyTrends);

public record MonthlyHiringTrendResponse(
    string Month,
    int RolesOpened,
    int RolesFilled,
    int HiringGoalOpened,
    decimal AverageTimeToFill,
    int SourceHires,
    int ReferralHires);
