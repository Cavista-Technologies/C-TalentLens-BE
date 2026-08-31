using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record CreateActionItemRequest(
    [Required, MaxLength(500)] string Description,
    Guid OwnerUserId,
    DateOnly? DueDate,
    [MaxLength(160)] string? Title = null,
    ActionItemCategory Category = ActionItemCategory.Other,
    [MaxLength(120)] string? CustomCategory = null,
    ActionItemPriority Priority = ActionItemPriority.Medium);

public record CompleteActionItemRequest(
    [MaxLength(1000)] string? CompletionNotes = null);

public record UpdateActionItemStatusRequest(
    ActionItemStatus Status,
    [MaxLength(1000)] string? Notes = null);

public record ReassignActionItemRequest(
    Guid OwnerUserId,
    [MaxLength(1000)] string? Notes = null);
