using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record CreateRequisitionRequest(
    [Required, MaxLength(40)] string RequisitionCode,
    [Required, MaxLength(160)] string RoleName,
    RecruitmentTeam Department,
    Guid HiringManagerUserId,
    Guid RecruiterUserId,
    RequisitionPriority Priority,
    DateOnly DateOpened,
    [Range(1, 1000)] int HiringGoal,
    RequisitionOpeningReason OpeningReason = RequisitionOpeningReason.Other,
    [MaxLength(120)] string? CustomOpeningReason = null,
    PostingType PostingType = PostingType.External,
    [MaxLength(1000)] string? StatusComment = null,
    [MaxLength(1000)] string? HiringManagerNotes = null);
