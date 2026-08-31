using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using C_TalentLens;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Integrations.SmartRecruiters;
using C_TalentLens.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace C_TalentLens.Tests;

public class TalentLensApiIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"talentlens-tests-{Guid.NewGuid():N}.db");
    private readonly string _hangfireDatabasePath = Path.Combine(
        Path.GetTempPath(),
        $"talentlens-hangfire-tests-{Guid.NewGuid():N}.db");

    private readonly WebApplicationFactory<Program> _factory;

    public TalentLensApiIntegrationTests()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__TalentLens", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Hangfire__DatabasePath", _hangfireDatabasePath);
        Environment.SetEnvironmentVariable("BackgroundJobs__Enabled", "false");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-only-signing-key-for-talentlens-integration");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TalentLens"] = $"Data Source={_databasePath}",
                        ["Hangfire:DatabasePath"] = _hangfireDatabasePath,
                        ["BackgroundJobs:Enabled"] = "false",
                        ["Jwt:SigningKey"] = "test-only-signing-key-for-talentlens-integration"
                    });
                });
            });
    }

    [Fact]
    public async Task GetDashboard_ReturnsSeededRecruitmentMetrics()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(11, dashboard.RecruitmentOverview.TotalOpenRoles);
        Assert.Equal(5, dashboard.RecruitmentOverview.ClosedRoles);
        Assert.Equal(22, dashboard.RecruitmentOverview.TotalHiringGoals);
        Assert.Equal(6, dashboard.RecruitmentOverview.GoalsFilled);
        Assert.Equal(16, dashboard.RecruitmentOverview.OutstandingGoals);
        Assert.Equal(19.6m, dashboard.TimeToFill.AverageTimeToFill);
        Assert.Equal(4, dashboard.Pipeline.RolesInPipeliningSourcing);
        Assert.Equal(3, dashboard.Pipeline.RolesInInterviewStage);
        Assert.Equal(2, dashboard.Pipeline.RolesInRequestToHire);
        Assert.Equal(0, dashboard.Pipeline.RolesInSparkHire);
        Assert.Equal(0, dashboard.Pipeline.RolesOfferedOrHired);
        Assert.Equal(5, dashboard.Pipeline.RolesFilled);
        Assert.Equal(9.1m, dashboard.SlaCompliance.ComplianceRate);
        Assert.Contains(dashboard.RecruiterPerformance.Scorecards, item =>
            item.RecruiterName == "Maya Chen" &&
            item.ActiveRequisitions == 6 &&
            item.OpenBottlenecks == 2);
        Assert.Equal(3, dashboard.Bottlenecks.TotalOpenBottlenecks);
        Assert.Contains(dashboard.Bottlenecks.ByCategory, item => item.Name == BottleneckCategory.HiringManagerDelay.ToString());
        Assert.Equal(5, dashboard.Actions.TotalOpenActions);
        Assert.Contains(dashboard.Actions.ByCategory, item => item.Name == ActionItemCategory.HiringManagerFeedback.ToString());
        Assert.Contains(dashboard.Actions.ByPriority, item => item.Name == ActionItemPriority.High.ToString());
        Assert.NotEmpty(dashboard.Risk.Items);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetStringAsync("/health");

        Assert.Equal("Healthy", response);
    }

    [Fact]
    public async Task GetRisks_ReturnsActiveRequisitionsOrderedByRisk()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var risksPage = await client.GetFromJsonAsync<PagedResponse<RiskAssessmentResponse>>("/api/risks", JsonOptions);

        Assert.NotNull(risksPage);
        var risks = risksPage.Items.ToList();
        Assert.True(risksPage.Success);
        Assert.Equal(11, risksPage.Pagination.TotalItems);
        Assert.Equal(11, risks.Count);
        Assert.True(risks[0].RiskScore >= risks[1].RiskScore);
        Assert.Equal("REQ-2026-009", risks[0].RequisitionCode);
        Assert.Equal("Critical", risks[0].RiskLevel.ToString());
        Assert.NotEmpty(risks[0].Factors);
    }

    [Fact]
    public async Task GetRiskDashboard_ReturnsGlobalRiskView()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var dashboard = await client.GetFromJsonAsync<GlobalRiskDashboardResponse>("/api/risks/dashboard", JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(11, dashboard.Summary.TotalActiveRequisitions);
        Assert.Equal(
            dashboard.Distribution.Medium + dashboard.Distribution.High + dashboard.Distribution.Critical,
            dashboard.Summary.TotalAtRiskRoles);
        Assert.True(dashboard.Summary.AverageRiskScore > 0);
        Assert.True(dashboard.Distribution.Critical > 0);
        Assert.Contains(dashboard.ByDepartment, item => item.Name == "Engineering");
        Assert.Contains(dashboard.ByRecruiter, item => item.Name == "Maya Chen");
        Assert.Contains(dashboard.ByHiringManager, item => item.Name == "Ada Okafor");
        Assert.Contains(dashboard.ByPriority, item => item.Name == RequisitionPriority.High.ToString());
        Assert.NotEmpty(dashboard.TopRiskDrivers);
        Assert.Contains(dashboard.HighRiskRequisitions, item =>
            item.RequisitionCode == "REQ-2026-001" &&
            item.HiringManager == "Ada Okafor" &&
            item.RiskLevel == RiskLevel.Critical);
        Assert.False(string.IsNullOrWhiteSpace(dashboard.TrendAnalysis.RiskTrend));
        Assert.True(dashboard.TrendAnalysis.WorseningRequisitions > 0);
    }

    [Fact]
    public async Task GetRiskDashboard_SupportsDepartmentFilter()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var dashboard = await client.GetFromJsonAsync<GlobalRiskDashboardResponse>("/api/risks/dashboard?department=Engineering", JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(3, dashboard.Summary.TotalActiveRequisitions);
        Assert.Single(dashboard.ByDepartment);
        Assert.Equal("Engineering", dashboard.ByDepartment.Single().Name);
        Assert.All(dashboard.HighRiskRequisitions, requisition => Assert.Equal(RecruitmentTeam.Engineering, requisition.Department));
    }

    [Fact]
    public async Task Login_ReturnsBearerTokenForSeededRecruiter()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            "maya.chen@talentlens.local",
            "Password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(login);
        Assert.Equal("Bearer", login.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Contains("Recruiter", login.User.Roles);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUserForBearerToken()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var profile = await client.GetFromJsonAsync<UserProfileResponse>("/api/auth/me", JsonOptions);

        Assert.NotNull(profile);
        Assert.Equal("maya.chen@talentlens.local", profile.Email);
        Assert.Equal("Taylor Morgan", profile.ReportingLine);
        Assert.Contains("Recruiter", profile.Roles);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsersFilteredByRole()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var usersPage = await client.GetFromJsonAsync<PagedResponse<UserSummaryResponse>>("/api/users?role=Recruiter", JsonOptions);

        Assert.NotNull(usersPage);
        var users = usersPage.Items.ToList();
        Assert.Contains(users, user => user.Email == "maya.chen@talentlens.local");
        Assert.Contains(users, user => user.Email == "noah.bello@talentlens.local");
        Assert.All(users, user => Assert.Contains(UserRole.Recruiter, user.Roles));
    }

    [Fact]
    public async Task GetUsers_SupportsDirectorySearch()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var usersPage = await client.GetFromJsonAsync<PagedResponse<UserSummaryResponse>>("/api/users?search=Engineering", JsonOptions);

        Assert.NotNull(usersPage);
        var users = usersPage.Items.ToList();
        Assert.Contains(users, user => user.FullName == "Ada Okafor");
        Assert.All(users, user => Assert.Contains("Engineering", user.Department ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetRequisitions_SupportsSearchAndFilters()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var filtered = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>(
            "/api/requisitions?search=backend&priority=High&openOnly=true",
            JsonOptions);

        Assert.NotNull(filtered);
        var requisitions = filtered.Items.ToList();
        Assert.Single(requisitions);
        Assert.Equal("REQ-2026-001", requisitions[0].RequisitionCode);
        Assert.Equal(RequisitionOpeningReason.Expansion, requisitions[0].OpeningReason);
        Assert.Equal(PostingType.External, requisitions[0].PostingType);
        Assert.False(string.IsNullOrWhiteSpace(requisitions[0].HiringManagerNotes));
    }

    [Fact]
    public async Task GetRequisitions_ScopesRecruiterToAssignedRoles()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var page = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions", JsonOptions);

        Assert.NotNull(page);
        Assert.Equal(8, page.Pagination.TotalItems);
        Assert.All(page.Items, requisition => Assert.Equal(login.User.Id, requisition.RecruiterUserId));
    }

    [Fact]
    public async Task GetDashboard_ScopesHiringManagerToOwnedRoles()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var dashboard = await client.GetFromJsonAsync<DashboardResponse>("/api/dashboard", JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(4, dashboard.RecruitmentOverview.TotalOpenRoles);
        Assert.Equal(7, dashboard.RecruitmentOverview.TotalHiringGoals);
        Assert.Equal(2, dashboard.Pipeline.RolesInInterviewStage);
    }

    [Fact]
    public async Task SourceAnalytics_ReturnsSourceContributionAndConversion()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var analytics = await client.GetFromJsonAsync<SourceAnalyticsResponse>("/api/source-analytics", JsonOptions);

        Assert.NotNull(analytics);
        Assert.Equal(17, analytics.TotalSourceActivities);
        Assert.Equal(3, analytics.TotalHires);
        Assert.Equal(17.6m, analytics.OverallConversionRate);
        Assert.Contains(analytics.Sources, source =>
            source.Source == HireSource.Referral.ToString() &&
            source.Hires == 2 &&
            source.SourceContributionPercentage == 66.7m);
        Assert.NotEmpty(analytics.MonthlyTrends);
    }

    [Fact]
    public async Task ReferralAnalytics_ReturnsSubmissionConversionAndDepartmentMetrics()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var analytics = await client.GetFromJsonAsync<ReferralAnalyticsResponse>("/api/referral-analytics", JsonOptions);

        Assert.NotNull(analytics);
        Assert.Equal(12, analytics.TotalReferralsSubmitted);
        Assert.Equal(2, analytics.ReferralHires);
        Assert.Equal(16.7m, analytics.OverallConversionRate);
        Assert.Contains(analytics.ByReferrerDepartment, item =>
            item.Department == "People Analytics" &&
            item.ReferralHires == 1);
        Assert.NotEmpty(analytics.MonthlyTrends);
    }

    [Fact]
    public async Task Referrals_CanBeCreatedForScopedRequisition()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisitions = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions?search=Regional", JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions.Items);

        var response = await client.PostAsJsonAsync("/api/referrals", new CreateReferralRequest(
            requisition.Id,
            "Amina Yusuf",
            "Sales",
            "Jordan Kim",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ReferralStatus.Submitted,
            ReferralHiringOutcome.Pending,
            null,
            "EMP-4455",
            "jordan.kim@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var referral = await response.Content.ReadFromJsonAsync<ReferralResponse>(JsonOptions);
        Assert.NotNull(referral);
        Assert.Equal(requisition.Id, referral.RequisitionId);
        Assert.Equal("Regional Sales Manager", referral.RoleAppliedFor);
        Assert.Equal("jordan.kim@example.com", referral.CandidateEmail);
        Assert.Equal("EMP-4455", referral.ReferrerEmployeeId);
        Assert.Equal(ReferralHiringOutcome.Pending, referral.HiringOutcome);
    }

    [Fact]
    public async Task PublicReferrals_CanSearchOpenRequisitionsAndSubmitWithCompanyEmail()
    {
        var client = _factory.CreateClient();
        var requisitions = await client.GetFromJsonAsync<IReadOnlyCollection<PublicRequisitionResponse>>(
            "/api/public/requisitions?search=Regional",
            JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions);

        var response = await client.PostAsJsonAsync("/api/public/referrals", new CreatePublicReferralRequest(
            requisition.Id,
            "Amina Yusuf",
            "amina.yusuf@cavista.com",
            "Sales",
            "Jordan Kim",
            "jordan.kim@example.com",
            "555-0188",
            "https://res.cloudinary.com/jtegygcp/raw/upload/resume.pdf",
            "Former colleague",
            "2 years",
            "Strong sales leadership background."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var referral = await response.Content.ReadFromJsonAsync<ReferralResponse>(JsonOptions);
        Assert.NotNull(referral);
        Assert.Equal(requisition.Id, referral.RequisitionId);
        Assert.Equal("Amina Yusuf", referral.ReferrerName);
        Assert.Equal("amina.yusuf@cavista.com", referral.SubmitterEmail);
        Assert.Equal(ReferralStatus.Submitted, referral.Status);
        Assert.Equal(ReferralHiringOutcome.Pending, referral.HiringOutcome);
        Assert.Contains(referral.History, history => history.ChangedBy == "Referral Portal");
    }

    [Fact]
    public async Task PublicReferrals_RequireCavistaReferrerEmail()
    {
        var client = _factory.CreateClient();
        var requisitions = await client.GetFromJsonAsync<IReadOnlyCollection<PublicRequisitionResponse>>(
            "/api/public/requisitions?search=Regional",
            JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions);

        var response = await client.PostAsJsonAsync("/api/public/referrals", new CreatePublicReferralRequest(
            requisition.Id,
            "Amina Yusuf",
            "amina@example.com",
            "Sales",
            "Jordan Kim",
            "jordan.kim@example.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReferralStatusUpdate_TracksOutcomeHistoryAndSupportsFiltering()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisitions = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions?search=Regional", JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions.Items);

        var created = await client.PostAsJsonAsync("/api/referrals", new CreateReferralRequest(
            requisition.Id,
            "Amina Yusuf",
            "Sales",
            "Casey Stone",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ReferralStatus.Submitted,
            ReferralHiringOutcome.Pending,
            null,
            "EMP-4456",
            "casey.stone@example.com"));
        await AssertSuccessAsync(created);
        var referral = await created.Content.ReadFromJsonAsync<ReferralResponse>(JsonOptions);
        Assert.NotNull(referral);

        var updated = await client.PatchAsJsonAsync(
            $"/api/referrals/{referral.Id}/status",
            new UpdateReferralStatusRequest(
                ReferralStatus.Hired,
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Candidate accepted offer."));
        await AssertSuccessAsync(updated);
        var hired = await updated.Content.ReadFromJsonAsync<ReferralResponse>(JsonOptions);

        Assert.NotNull(hired);
        Assert.Equal(ReferralStatus.Hired, hired.Status);
        Assert.Equal(ReferralHiringOutcome.Hired, hired.HiringOutcome);
        Assert.Contains(hired.History, item => item.EventType == ReferralEventType.Created);
        Assert.Contains(hired.History, item => item.EventType == ReferralEventType.StatusChanged);
        Assert.Contains(hired.History, item => item.EventType == ReferralEventType.OutcomeChanged);

        var filtered = await client.GetFromJsonAsync<PagedResponse<ReferralResponse>>(
            "/api/referrals?status=Hired&search=casey",
            JsonOptions);
        Assert.NotNull(filtered);
        Assert.Contains(filtered.Items, item => item.Id == referral.Id);
    }

    [Fact]
    public async Task ReferralImport_CreatesRowsFromSpreadsheetJson()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);

        var response = await client.PostAsJsonAsync("/api/imports/referrals", new[]
        {
            new ReferralImportRowRequest(
                null,
                "Regional Sales Manager",
                "amina.yusuf@company.local",
                "Amina Yusuf",
                "Legacy Candidate",
                null,
                null,
                "https://sharepoint.local/cv/legacy-candidate.pdf",
                "Previously worked together",
                "4 years",
                "Strong sales leadership background.",
                DateTimeOffset.UtcNow.AddMinutes(-4),
                DateTimeOffset.UtcNow,
                ReferralStatus.Submitted,
                ReferralHiringOutcome.Pending)
        });

        await AssertSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var referrals = await client.GetFromJsonAsync<PagedResponse<ReferralResponse>>("/api/referrals?search=legacy", JsonOptions);
        Assert.NotNull(referrals);
        var referral = Assert.Single(referrals.Items);
        Assert.Equal("Legacy Candidate", referral.CandidateName);
        Assert.Equal("https://sharepoint.local/cv/legacy-candidate.pdf", referral.ResumeUrl);
        Assert.Equal("Previously worked together", referral.CandidateRelationship);
        Assert.EndsWith("@legacy-referral.local", referral.CandidateEmail);
    }

    [Fact]
    public async Task RequisitionImport_CreatesRequisitionFromSpreadsheetJson()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);

        var response = await client.PostAsJsonAsync("/api/imports/requisitions", new[]
        {
            new RequisitionImportRowRequest(
                "REQ-IMPORT-001",
                "Growth Marketing Manager",
                "Marketing and Communications",
                "Expansion",
                "Ada Okafor",
                "External",
                "Sourcing just started.",
                "Look for B2B SaaS demand generation experience.",
                null,
                null,
                RequisitionPriority.Medium,
                DateOnly.FromDateTime(DateTime.UtcNow),
                null,
                1,
                0,
                RequisitionStatus.Active,
                PipelineStage.JobPosting)
        });

        await AssertSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var requisitions = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions?search=Growth", JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions.Items);
        Assert.Equal("REQ-IMPORT-001", requisition.RequisitionCode);
        Assert.Equal(RequisitionOpeningReason.Expansion, requisition.OpeningReason);
        Assert.Equal(PostingType.External, requisition.PostingType);
        Assert.Equal("Look for B2B SaaS demand generation experience.", requisition.HiringManagerNotes);
    }

    [Fact]
    public async Task SourceActivities_CanBeCreatedForScopedRequisition()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisitions = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions?search=Regional", JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions.Items);

        var response = await client.PostAsJsonAsync("/api/source-activities", new CreateSourceActivityRequest(
            requisition.Id,
            "Jordan Kim",
            HireSource.Other,
            "Agency",
            DateOnly.FromDateTime(DateTime.UtcNow),
            SourceActivityStatus.Submitted,
            null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var activity = await response.Content.ReadFromJsonAsync<SourceActivityResponse>(JsonOptions);
        Assert.NotNull(activity);
        Assert.Equal(HireSource.Other, activity.Source);
        Assert.Equal("Other", activity.SourceLabel);
        Assert.Equal(requisition.Id, activity.RequisitionId);
    }

    [Fact]
    public async Task HiringTrends_ReturnsMonthlyTrendData()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var trends = await client.GetFromJsonAsync<HiringTrendResponse>("/api/analytics/hiring-trends", JsonOptions);

        Assert.NotNull(trends);
        Assert.NotEmpty(trends.MonthlyTrends);
        Assert.Contains(trends.MonthlyTrends, trend => trend.RolesOpened > 0);
        Assert.Contains(trends.MonthlyTrends, trend => trend.ReferralHires > 0);
    }

    [Fact]
    public async Task LeadershipSummary_CombinesExecutiveRiskTrendAndSourceMetrics()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "leadership@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var summary = await client.GetFromJsonAsync<LeadershipSummaryResponse>("/api/leadership-summary", JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(11, summary.ExecutiveKpis.TotalOpenRoles);
        Assert.Equal(22, summary.ExecutiveKpis.TotalHiringGoals);
        Assert.Equal(19.6m, summary.ExecutiveKpis.AverageTimeToFill);
        Assert.Equal(27.3m, summary.HiringProgress.HiringGoalAchievementRate);
        Assert.Equal(11, summary.RecruitmentOverview.TotalOpenRoles);
        Assert.True(summary.SlaCompliance.ComplianceRate > 0);
        Assert.True(summary.RiskSummary.TotalAtRiskRequisitions > 0);
        Assert.True(summary.RiskSummary.AverageRiskScore > 0);
        Assert.NotEmpty(summary.RiskSummary.TopRiskDrivers);
        Assert.NotEmpty(summary.RiskSummary.CriticalRiskSpotlight);
        Assert.True(summary.RiskSummary.OpenBottlenecks > 0);
        Assert.False(string.IsNullOrWhiteSpace(summary.TalentSourcePerformance.TopHiringSource));
        Assert.False(string.IsNullOrWhiteSpace(summary.TalentSourcePerformance.TopReferringDepartment));
        Assert.NotEmpty(summary.TalentSourcePerformance.SourceRanking);
        Assert.Equal(17, summary.SourceOfHire.TotalSourceActivities);
        Assert.Equal(12, summary.ReferralPerformance.TotalReferralsSubmitted);
        Assert.Equal(2, summary.ReferralPerformance.ReferralHires);
        Assert.NotEmpty(summary.Insights);
        Assert.NotEmpty(summary.HiringTrends.MonthlyTrends);
    }

    [Fact]
    public async Task GetUsers_RequiresAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequisition_RequiresAuthorization()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/requisitions", new CreateRequisitionRequest(
            "REQ-AUTH-001",
            "Platform Engineer",
            RecruitmentTeam.Engineering,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            RequisitionPriority.High,
            new DateOnly(2026, 8, 17),
            1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequisition_AllowsRecruiterToken()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/requisitions", new CreateRequisitionRequest(
            "REQ-AUTH-002",
            "Platform Engineer",
            RecruitmentTeam.Engineering,
            hiringManager.User.Id,
            login.User.Id,
            RequisitionPriority.High,
            new DateOnly(2026, 8, 17),
            1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequisition_DuplicateCodeReturnsConflictProblemDetails()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        await CreateRequisitionAsync(client, login.User.Id, hiringManager.User.Id, "REQ-CONFLICT-001");

        var response = await client.PostAsJsonAsync("/api/requisitions", new CreateRequisitionRequest(
            "REQ-CONFLICT-001",
            "Platform Engineer",
            RecruitmentTeam.Engineering,
            hiringManager.User.Id,
            login.User.Id,
            RequisitionPriority.High,
            new DateOnly(2026, 8, 17),
            1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("duplicate_requisition_code", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task ReassignRecruiter_OnlyAllowsTalentAcquisitionManager()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        var taManager = await LoginAsync(client, "ta.manager@talentlens.local");
        var newRecruiter = await LoginAsync(client, "noah.bello@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-REASSIGN-001");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var forbidden = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/recruiter",
            new ReassignRequisitionRecruiterRequest(newRecruiter.User.Id));

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", taManager.AccessToken);
        var allowed = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/recruiter",
            new ReassignRequisitionRecruiterRequest(newRecruiter.User.Id));

        await AssertSuccessAsync(allowed);
        var updated = await allowed.Content.ReadFromJsonAsync<RequisitionResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(newRecruiter.User.Id, updated.RecruiterUserId);
    }

    [Fact]
    public async Task SmartRecruitersSync_RequiresAuthorization()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/integrations/smartrecruiters/jobs/sync", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SmartRecruitersSync_CreatesAndUpdatesRequisitions()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var firstResponse = await client.PostAsync("/api/integrations/smartrecruiters/jobs/sync", null);
        await AssertSuccessAsync(firstResponse);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SmartRecruitersSyncResponse>(JsonOptions);

        Assert.NotNull(firstResult);
        Assert.Equal(4, firstResult.TotalJobs);
        Assert.Equal(4, firstResult.CreatedCount);
        Assert.Equal(0, firstResult.UpdatedCount);
        Assert.Empty(firstResult.Errors);

        var secondResponse = await client.PostAsync("/api/integrations/smartrecruiters/jobs/sync", null);
        await AssertSuccessAsync(secondResponse);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<SmartRecruitersSyncResponse>(JsonOptions);

        Assert.NotNull(secondResult);
        Assert.Equal(4, secondResult.TotalJobs);
        Assert.Equal(0, secondResult.CreatedCount);
        Assert.Equal(4, secondResult.UpdatedCount);

        var requisitions = await client.GetFromJsonAsync<PagedResponse<RequisitionResponse>>("/api/requisitions?search=Senior%20Platform", JsonOptions);
        Assert.NotNull(requisitions);
        var requisition = Assert.Single(requisitions.Items);
        Assert.StartsWith("REQ-SR-", requisition.RequisitionCode);
        Assert.Equal("Senior Platform Engineer", requisition.RoleName);
        Assert.Equal(RecruitmentTeam.Engineering, requisition.Department);
        Assert.Equal(PostingType.External, requisition.PostingType);
        Assert.Equal(PipelineStage.JobPosting, requisition.CurrentStage);
        Assert.Equal(RequisitionStatus.Active, requisition.CurrentStatus);
    }

    [Fact]
    public async Task GetAlerts_RequiresAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/alerts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAlerts_ForbidsRecruiterFromGlobalFeed()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "maya.chen@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/alerts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAlerts_AllowsLeadershipGlobalFeed()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "leadership@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var alertsPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts", JsonOptions);

        Assert.NotNull(alertsPage);
        var alerts = alertsPage.Items.ToList();
        Assert.Contains(alerts, alert => alert.Type == AlertType.SlaBreached &&
                                         alert.RecipientRole == UserRole.Recruiter &&
                                         alert.RequisitionCode == "REQ-2026-001");
        Assert.Contains(alerts, alert => alert.Type == AlertType.CriticalRisk &&
                                         alert.RecipientRole == UserRole.TalentAcquisitionManager &&
                                         alert.RequisitionCode == "REQ-2026-001");
        Assert.Contains(alerts, alert => alert.Type == AlertType.OpenBottleneck &&
                                         alert.RecipientRole == UserRole.HiringManager &&
                                         alert.RecipientName == "Ada Okafor" &&
                                         alert.Severity == AlertSeverity.Critical &&
                                         alert.Metadata["escalationLevel"] == "Critical");
    }

    [Fact]
    public async Task GetAlerts_SupportsSeverityFilter()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsync(client, "leadership@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var alertsPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts?severity=Critical", JsonOptions);

        Assert.NotNull(alertsPage);
        var alerts = alertsPage.Items.ToList();
        Assert.NotEmpty(alerts);
        Assert.All(alerts, alert => Assert.Equal(AlertSeverity.Critical, alert.Severity));
    }

    [Fact]
    public async Task GetMyAlerts_ReturnsOnlyCurrentUsersAlerts()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var alertsPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me", JsonOptions);

        Assert.NotNull(alertsPage);
        var alerts = alertsPage.Items.ToList();
        Assert.NotEmpty(alerts);
        Assert.All(alerts, alert => Assert.Equal(login.User.Id, alert.RecipientUserId));
        Assert.Contains(alerts, alert => alert.Type == AlertType.SlaBreached &&
                                         alert.RequisitionCode == "REQ-2026-001");
    }

    [Fact]
    public async Task AssignmentNotifications_AreCreatedForRequisitionsBottlenecksAndActions()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var newRecruiter = await LoginAsync(client, "noah.bello@talentlens.local");
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        var taManager = await LoginAsync(client, "ta.manager@talentlens.local");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-NOTIFY-001");

        var recruiterAlerts = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>(
            "/api/alerts/me?type=RequisitionAssigned",
            JsonOptions);
        Assert.NotNull(recruiterAlerts);
        Assert.Contains(recruiterAlerts.Items, alert =>
            alert.Type == AlertType.RequisitionAssigned &&
            alert.RequisitionId == requisition.Id &&
            alert.RecipientUserId == recruiter.User.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", taManager.AccessToken);
        var reassigned = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/recruiter",
            new ReassignRequisitionRecruiterRequest(newRecruiter.User.Id));
        await AssertSuccessAsync(reassigned);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newRecruiter.AccessToken);
        var newRecruiterAlerts = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>(
            "/api/alerts/me?type=RequisitionAssigned",
            JsonOptions);
        Assert.NotNull(newRecruiterAlerts);
        Assert.Contains(newRecruiterAlerts.Items, alert =>
            alert.Type == AlertType.RequisitionAssigned &&
            alert.RequisitionId == requisition.Id &&
            alert.RecipientUserId == newRecruiter.User.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newRecruiter.AccessToken);
        var bottleneckResponse = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks",
            new CreateBottleneckRequest(
                "Interview feedback is delayed.",
                hiringManager.User.Id,
                BottleneckCategory.HiringManagerDelay,
                null,
                "Feedback has not been submitted by the panel.",
                BottleneckPriority.High,
                "The role cannot progress without feedback."));
        await AssertSuccessAsync(bottleneckResponse);
        var bottleneck = await bottleneckResponse.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(bottleneck);

        var actionResponse = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions",
            new CreateActionItemRequest(
                "Submit interview feedback.",
                hiringManager.User.Id,
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
                "Submit feedback",
                ActionItemCategory.HiringManagerFeedback,
                null,
                ActionItemPriority.High));
        await AssertSuccessAsync(actionResponse);
        var action = await actionResponse.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(action);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var ownerAlerts = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me", JsonOptions);
        Assert.NotNull(ownerAlerts);
        Assert.Contains(ownerAlerts.Items, alert =>
            alert.Type == AlertType.BottleneckAssigned &&
            alert.Metadata["bottleneckId"] == bottleneck.Id.ToString());
        Assert.Contains(ownerAlerts.Items, alert =>
            alert.Type == AlertType.ActionAssigned &&
            alert.Metadata["actionItemId"] == action.Id.ToString());
    }

    [Fact]
    public async Task AlertNotifications_CanBeMarkedReadAndUnread()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsRecruiterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var alertsPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?unreadOnly=true", JsonOptions);

        Assert.NotNull(alertsPage);
        var alert = Assert.Single(alertsPage.Items, item =>
            item.Type == AlertType.SlaBreached &&
            item.RequisitionCode == "REQ-2026-001");
        Assert.False(alert.IsRead);
        Assert.Equal(NotificationStatus.Unread, alert.NotificationStatus);

        var readResponse = await client.PatchAsync($"/api/alerts/{alert.NotificationId}/read", null);
        await AssertSuccessAsync(readResponse);
        var readAlert = await readResponse.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.NotNull(readAlert);
        Assert.True(readAlert.IsRead);
        Assert.Equal(NotificationStatus.Read, readAlert.NotificationStatus);
        Assert.NotNull(readAlert.ReadAt);

        var unreadPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?unreadOnly=true", JsonOptions);
        Assert.NotNull(unreadPage);
        Assert.DoesNotContain(unreadPage.Items, item => item.NotificationId == alert.NotificationId);

        var unreadResponse = await client.PatchAsync($"/api/alerts/{alert.NotificationId}/unread", null);
        await AssertSuccessAsync(unreadResponse);
        var unreadAlert = await unreadResponse.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.NotNull(unreadAlert);
        Assert.False(unreadAlert.IsRead);
        Assert.Equal(NotificationStatus.Unread, unreadAlert.NotificationStatus);
        Assert.Null(unreadAlert.ReadAt);
    }

    [Fact]
    public async Task ResolveBottleneck_RemovesOpenBottleneckAlert()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-BLOCKER-001");

        var created = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks",
            new CreateBottleneckRequest(
                "Hiring manager feedback is delayed.",
                hiringManager.User.Id,
                BottleneckCategory.HiringManagerDelay,
                null,
                "Hiring manager feedback has not been submitted.",
                BottleneckPriority.High,
                "The requisition cannot progress to offer decision."));
        await AssertSuccessAsync(created);
        var bottleneck = await created.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(bottleneck);
        Assert.Equal(BottleneckCategory.HiringManagerDelay, bottleneck.Category);
        Assert.Equal(BottleneckPriority.High, bottleneck.Priority);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var beforePage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?type=OpenBottleneck", JsonOptions);
        Assert.NotNull(beforePage);
        var before = beforePage.Items.ToList();
        Assert.Contains(before, alert => alert.Metadata["bottleneckId"] == bottleneck.Id.ToString());

        var updatedStatus = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks/{bottleneck.Id}/status",
            new UpdateBottleneckStatusRequest(BlockerStatus.InProgress));
        Assert.Equal(HttpStatusCode.OK, updatedStatus.StatusCode);
        var inProgress = await updatedStatus.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(inProgress);
        Assert.Equal(BlockerStatus.InProgress, inProgress.Status);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var unauthorizedResolve = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks/{bottleneck.Id}/resolve",
            JsonContent.Create(new ResolveBottleneckRequest("Recruiter tried to resolve it.")));
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedResolve.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var resolved = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks/{bottleneck.Id}/resolve",
            JsonContent.Create(new ResolveBottleneckRequest("Hiring manager feedback was submitted.")));
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        var resolvedBottleneck = await resolved.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(resolvedBottleneck);
        Assert.Equal(BlockerStatus.Resolved, resolvedBottleneck.Status);
        Assert.NotNull(resolvedBottleneck.ResolvedAt);
        Assert.Equal("Hiring manager feedback was submitted.", resolvedBottleneck.ResolutionSummary);
        Assert.Equal(hiringManager.User.Id, resolvedBottleneck.ResolutionOwnerUserId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var afterPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?type=OpenBottleneck", JsonOptions);
        Assert.NotNull(afterPage);
        var after = afterPage.Items.ToList();
        Assert.DoesNotContain(after, alert => alert.Metadata.TryGetValue("bottleneckId", out var id) &&
                                             id == bottleneck.Id.ToString());
    }

    [Fact]
    public async Task ResolveBottleneck_AllowsTalentAcquisitionManagerOverride()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        var taManager = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-BLOCKER-002");

        var created = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks",
            new CreateBottleneckRequest(
                "Approval is delayed.",
                hiringManager.User.Id,
                BottleneckCategory.ApprovalDelay,
                null,
                "Approval has not been completed.",
                BottleneckPriority.High,
                "The requisition cannot progress."));
        await AssertSuccessAsync(created);
        var bottleneck = await created.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(bottleneck);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", taManager.AccessToken);
        var resolved = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/bottlenecks/{bottleneck.Id}/resolve",
            JsonContent.Create(new ResolveBottleneckRequest("TA manager escalated and cleared the approval.")));

        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        var resolvedBottleneck = await resolved.Content.ReadFromJsonAsync<BottleneckResponse>(JsonOptions);
        Assert.NotNull(resolvedBottleneck);
        Assert.Equal(BlockerStatus.Resolved, resolvedBottleneck.Status);
        Assert.Equal(taManager.User.Id, resolvedBottleneck.ResolutionOwnerUserId);
    }

    [Fact]
    public async Task CompleteAction_RemovesOverdueActionAlert()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-ACTION-001");

        var created = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions",
            new CreateActionItemRequest("Follow up with candidate.", recruiter.User.Id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)));
        await AssertSuccessAsync(created);
        var action = await created.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(action);

        var beforePage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?type=OverdueAction", JsonOptions);
        Assert.NotNull(beforePage);
        var before = beforePage.Items.ToList();
        Assert.Contains(before, alert => alert.Metadata["actionItemId"] == action.Id.ToString());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hiringManager.AccessToken);
        var unauthorizedComplete = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/actions/{action.Id}/complete",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedComplete.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var completed = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/actions/{action.Id}/complete",
            null);
        await AssertSuccessAsync(completed);
        var completedAction = await completed.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(completedAction);
        Assert.Equal(ActionItemStatus.Completed, completedAction.Status);
        Assert.NotNull(completedAction.CompletedAt);
        Assert.Equal(recruiter.User.Id, completedAction.CompletedByUserId);

        var afterPage = await client.GetFromJsonAsync<PagedResponse<AlertResponse>>("/api/alerts/me?type=OverdueAction", JsonOptions);
        Assert.NotNull(afterPage);
        var after = afterPage.Items.ToList();
        Assert.DoesNotContain(after, alert => alert.Metadata.TryGetValue("actionItemId", out var id) &&
                                             id == action.Id.ToString());
    }

    [Fact]
    public async Task CompleteAction_AllowsTalentAcquisitionManagerOverride()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        var taManager = await LoginAsync(client, "ta.manager@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-ACTION-003");

        var created = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions",
            new CreateActionItemRequest("Confirm interview availability.", recruiter.User.Id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)));
        await AssertSuccessAsync(created);
        var action = await created.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(action);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", taManager.AccessToken);
        var completed = await client.PatchAsync(
            $"/api/requisitions/{requisition.Id}/actions/{action.Id}/complete",
            null);

        await AssertSuccessAsync(completed);
        var completedAction = await completed.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(completedAction);
        Assert.Equal(ActionItemStatus.Completed, completedAction.Status);
        Assert.Equal(taManager.User.Id, completedAction.CompletedByUserId);
    }

    [Fact]
    public async Task ActionStatusAndOwnerUpdates_RetainHistory()
    {
        var client = _factory.CreateClient();
        var recruiter = await LoginAsRecruiterAsync(client);
        var hiringManager = await LoginAsync(client, "ada.okafor@talentlens.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recruiter.AccessToken);
        var requisition = await CreateRequisitionAsync(client, recruiter.User.Id, hiringManager.User.Id, "REQ-ACTION-002");

        var created = await client.PostAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions",
            new CreateActionItemRequest(
                "Schedule panel interview.",
                recruiter.User.Id,
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
                "Schedule panel",
                ActionItemCategory.InterviewScheduling,
                null,
                ActionItemPriority.High));
        await AssertSuccessAsync(created);
        var action = await created.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);
        Assert.NotNull(action);

        var statusUpdated = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions/{action.Id}/status",
            new UpdateActionItemStatusRequest(ActionItemStatus.InProgress, "Panel availability requested."));
        await AssertSuccessAsync(statusUpdated);

        var reassigned = await client.PatchAsJsonAsync(
            $"/api/requisitions/{requisition.Id}/actions/{action.Id}/owner",
            new ReassignActionItemRequest(hiringManager.User.Id, "Hiring manager owns final panel schedule."));
        await AssertSuccessAsync(reassigned);
        var reassignedAction = await reassigned.Content.ReadFromJsonAsync<ActionItemResponse>(JsonOptions);

        Assert.NotNull(reassignedAction);
        Assert.Equal(hiringManager.User.Id, reassignedAction.OwnerUserId);
        Assert.Contains(reassignedAction.History, item => item.EventType == ActionItemEventType.Created);
        Assert.Contains(reassignedAction.History, item => item.EventType == ActionItemEventType.StatusChanged);
        Assert.Contains(reassignedAction.History, item => item.EventType == ActionItemEventType.OwnerChanged);
    }

    public void Dispose()
    {
        _factory.Dispose();

        DeleteIfExists(_databasePath);
        DeleteIfExists($"{_databasePath}-shm");
        DeleteIfExists($"{_databasePath}-wal");
        DeleteIfExists(_hangfireDatabasePath);
        DeleteIfExists($"{_hangfireDatabasePath}-shm");
        DeleteIfExists($"{_hangfireDatabasePath}-wal");
        Environment.SetEnvironmentVariable("ConnectionStrings__TalentLens", null);
        Environment.SetEnvironmentVariable("Hangfire__DatabasePath", null);
        Environment.SetEnvironmentVariable("BackgroundJobs__Enabled", null);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // SQLite can briefly hold file handles after the test host is disposed on Windows.
        }
    }

    private static async Task<LoginResponse> LoginAsRecruiterAsync(HttpClient client)
    {
        return await LoginAsync(client, "maya.chen@talentlens.local");
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            email,
            "Password123"));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return login ?? throw new InvalidOperationException("Login response was empty.");
    }

    private static async Task<RequisitionResponse> CreateRequisitionAsync(
        HttpClient client,
        Guid recruiterUserId,
        Guid hiringManagerUserId,
        string requisitionCode)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.PostAsJsonAsync("/api/requisitions", new CreateRequisitionRequest(
            requisitionCode,
            "Backend Engineer",
            RecruitmentTeam.Engineering,
            hiringManagerUserId,
            recruiterUserId,
            RequisitionPriority.High,
            today,
            1));
        response.EnsureSuccessStatusCode();

        var requisition = await response.Content.ReadFromJsonAsync<RequisitionResponse>(JsonOptions);
        return requisition ?? throw new InvalidOperationException("Create requisition response was empty.");
    }

    private static async Task AssertSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"{(int)response.StatusCode} {response.StatusCode}: {content}");
        }
    }
}

