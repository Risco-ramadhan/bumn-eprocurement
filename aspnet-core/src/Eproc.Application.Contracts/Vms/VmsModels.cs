using Eproc.Enums;

namespace Eproc.Vms;

public sealed record ShareholderBoDto(
    string ShareholderType, // "INDIVIDUAL" | "CORPORATE"
    string FullName,
    string IdentityNumberNikNpwp,
    decimal OwnershipPercentage,
    bool IsUltimateBeneficialOwner,
    bool IsPep
);

public sealed record BoardMemberDto(
    string PositionType, // "PRESIDENT_DIRECTOR" | "DIRECTOR" | "COMMISSIONER"
    string FullName,
    string NikPassport,
    bool IsAuthorizedSignatory
);

public sealed record VendorLegalDocDto(
    Guid Id,
    string DocType,
    string DocNumber,
    string IssuedBy,
    DateOnly IssuedDate,
    DateOnly? ExpiredDate,
    string VerificationStatus // "PENDING" | "VALID" | "REJECTED" | "EXPIRED"
);

public sealed record CsmsElementScoreDto(
    int ElementNumber,
    string ElementName,
    decimal WeightPct,
    decimal SelfAssessmentScore,
    decimal AuditorVerifiedScore
);

public sealed record CsmsCertificateSummaryDto(
    string CertificateNumber,
    CsmsRiskLevel QualifiedRiskLevel,
    decimal DocScore,
    decimal FieldAuditScore,
    decimal FinalCsmsScore,
    DateOnly IssuedDate,
    DateOnly ExpiredDate,
    bool IsActive
);

public sealed record VendorDetailDto(
    Guid Id,
    string VendorCode,
    string LegalEntityType,
    string CompanyName,
    string BusinessScale,
    string[] VendorRoleTypes,
    bool IsBumnAffiliated,
    bool IsPadiUmkm,
    string NibNumber,
    string NpwpNumber,
    bool IsPkp,
    string CityName,
    string CompanyEmail,
    VendorVerificationStatus VerificationStatus,
    decimal CurrentVpeScore,
    string CurrentVpeGrade,
    decimal NetWorthEquityKb,
    decimal HighestExperienceNpt,
    decimal KemampuanDasarKd,
    decimal ModalKerjaMk,
    decimal KemampuanNyataKn,
    decimal SisaKemampuanNyataSkn,
    int SisaKemampuanPaketSkp,
    decimal AltmanZScore,
    string FinancialHealthZone,
    List<ShareholderBoDto> Shareholders,
    List<BoardMemberDto> BoardMembers,
    List<VendorLegalDocDto> LegalDocuments,
    CsmsCertificateSummaryDto? ActiveCsmsCertificate
);

public sealed record RegisterVendorRequestDto(
    string LegalEntityType,
    string CompanyName,
    string BusinessScale,
    string[] VendorRoleTypes,
    bool IsBumnAffiliated,
    bool IsPadiUmkm,
    string NibNumber,
    string NpwpNumber,
    bool IsPkp,
    string CityName,
    string CompanyEmail,
    decimal CurrentAssets,
    decimal TotalAssets,
    decimal CurrentLiabilities,
    decimal TotalLiabilities,
    decimal NetWorthEquityKb,
    decimal RetainedEarnings,
    decimal Ebit,
    decimal HighestExperienceNpt,
    decimal TotalOngoingContractsValue,
    int ActiveOngoingPackagesCount,
    List<ShareholderBoDto> Shareholders,
    List<BoardMemberDto> BoardMembers
);

public sealed record SubmitCsmsAssessmentRequestDto(
    CsmsRiskLevel TargetRiskCategory,
    List<CsmsElementScoreDto> Elements,
    decimal FieldVerificationScore,
    string AuditorName
);

public sealed record AffiliationConflictItemDto(
    Guid VendorAId,
    string VendorAName,
    Guid VendorBId,
    string VendorBName,
    string ConflictRelationType, // "SHARED_ULTIMATE_BENEFICIAL_OWNER" | "SHARED_BOARD_DIRECTOR"
    string PersonName,
    string IdentityNumberNikNpwp
);

public sealed record RolePersonaDto(
    string RoleCode,
    string RoleName,
    string PortalTarget, // "PUBLIC_PORTAL" | "VENDOR_PORTAL" | "INTERNAL_BUMN_PORTAL"
    string PositionGrade,
    string[] ConflictingSodRoles,
    string[] GrantedPermissions
);
