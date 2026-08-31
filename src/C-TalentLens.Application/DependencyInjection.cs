using Microsoft.Extensions.DependencyInjection;

namespace C_TalentLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IRiskScoringEngine, RiskScoringEngine>();

        return services;
    }
}
