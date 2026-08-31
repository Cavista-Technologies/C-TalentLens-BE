using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record CreateBottleneckRequest(
    [Required, MaxLength(160)] string Reason,
    Guid OwnerUserId,
    BottleneckCategory Category = BottleneckCategory.Other,
    [MaxLength(120)] string? CustomCategory = null,
    [MaxLength(1000)] string? Description = null,
    BottleneckPriority Priority = BottleneckPriority.Medium,
    [MaxLength(1000)] string? BusinessImpact = null,
    DateOnly? DateIdentified = null);
