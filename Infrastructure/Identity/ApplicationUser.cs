using Microsoft.AspNetCore.Identity;

namespace C_TalentLens.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? ReportingLine { get; set; }
}
