using System.ComponentModel.DataAnnotations;

namespace C_TalentLens.Application.Dtos;

public record ResolveBottleneckRequest(
    [Required, MaxLength(1000)] string ResolutionSummary,
    [MaxLength(1000)] string? LessonsLearned = null);
