using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record UpdateRequisitionRequest(
    [Required, MaxLength(160)] string RoleName,
    RecruitmentTeam Department,
    Guid HiringManagerUserId,
    Guid RecruiterUserId,
    RequisitionPriority Priority,
    DateOnly DateOpened,
    [Range(1, 1000)] int HiringGoal,
    [Range(0, 1000)] int FilledGoal,
    RequisitionStatus CurrentStatus = RequisitionStatus.Active,
    DateOnly? ClosedDate = null,
    RequisitionOpeningReason OpeningReason = RequisitionOpeningReason.Other,
    [MaxLength(120)] string? CustomOpeningReason = null,
    PostingType PostingType = PostingType.External,
    [MaxLength(1000)] string? StatusComment = null,
    [MaxLength(1000)] string? HiringManagerNotes = null);
