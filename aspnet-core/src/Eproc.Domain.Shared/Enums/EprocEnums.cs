namespace Eproc.Enums;

public enum ProcurementCategory
{
    Goods = 1,
    Construction = 2,
    Consulting = 3,
    OtherServices = 4
}

public enum SourcingMethod
{
    TenderUmum = 1,
    TenderTerbatas = 2,
    SeleksiUmum = 3,
    BeautyContest = 4,
    PenunjukanLangsung = 5,
    PengadaanLangsung = 6,
    EPurchasingCatalog = 7
}

public enum SubmissionEnvelopeMethod
{
    SatuSampul = 1,
    DuaSampul = 2,
    DuaTahap = 3
}

public enum EvaluationMethod
{
    HargaTerendahGugur = 1,
    SistemNilaiScoring = 2,
    BiayaUmurEkonomisTco = 3,
    KualitasDanBiaya = 4,
    KualitasMurni = 5,
    PaguAnggaran = 6
}

public enum ContractType
{
    Lumsum = 1,
    HargaSatuan = 2,
    GabunganLumsumHargaSatuan = 3,
    TerimaJadiTurnkey = 4,
    KontrakPayungUmbrella = 5,
    WaktuPenugasan = 6
}

public enum CsmsRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Extreme = 4
}

public enum VendorVerificationStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    SiteVisitRequired = 3,
    VerifiedAvl = 4,
    Suspended = 5,
    Blacklisted = 6
}

public enum PphTaxType
{
    Pph22 = 1,
    Pph23 = 2,
    Pph4Ayat2Konstruksi = 3
}

public enum ScmLevel
{
    None = 0,
    ScmLevel1 = 1,
    ScmLevel2 = 2,
    ScmLevel3 = 3
}
