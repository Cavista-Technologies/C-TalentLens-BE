using System.Text.Json;
using C_TalentLens.Application.Integrations.SmartRecruiters;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Infrastructure.Integrations.SmartRecruiters;

public class MockSmartRecruitersClient(IOptions<SmartRecruitersOptions> options) : ISmartRecruitersClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyCollection<SmartRecruitersJob>> GetJobsAsync(CancellationToken cancellationToken)
    {
        var mockJobsFilePath = GetMockJobsFilePath(options.Value.MockJobsFilePath);
        await using var stream = File.OpenRead(mockJobsFilePath);
        var response = await JsonSerializer.DeserializeAsync<SmartRecruitersJobsResponse>(
            stream,
            JsonOptions,
            cancellationToken);

        return response?.Content ?? [];
    }

    private static string GetMockJobsFilePath(string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }
}
