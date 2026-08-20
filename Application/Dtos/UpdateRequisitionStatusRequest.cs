using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record UpdateRequisitionStatusRequest(
    RequisitionStatus Status,
    DateOnly? EffectiveDate);
