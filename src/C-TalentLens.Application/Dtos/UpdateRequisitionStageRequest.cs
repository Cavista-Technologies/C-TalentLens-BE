using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record UpdateRequisitionStageRequest(
    PipelineStage Stage,
    DateOnly? EffectiveDate);
