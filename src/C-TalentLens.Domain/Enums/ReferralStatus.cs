namespace C_TalentLens.Domain;

public enum ReferralStatus
{
    Submitted = 1,
    UnderReview = 2,
    Screening = 3,
    Interviewing = 4,
    Interview = Interviewing,
    OfferExtended = 5,
    Offer = OfferExtended,
    Hired = 6,
    Rejected = 7,
    Withdrawn = 8,
    Ineligible = 9
}
