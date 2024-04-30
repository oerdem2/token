using System.ComponentModel.DataAnnotations.Schema;

namespace amorphie.token.core;

public class KpsIdentity
{
    public KpsCertificationType CertificationType { get; set; }
    public long CitizenshipNumber { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string FatherName { get; set; }
    public long FatherCitizenshipNumber { get; set; }
    [NotMapped]
    public string FatherCitizenshipNumberMask { get; set; }
    public string MotherName { get; set; }
    public long MotherCitizenshipNumber { get; set; }
    [NotMapped]
    public string MotherCitizenshipNumberMask { get; set; }
    public long SpouseCitizenshipNumber { get; set; }
    [NotMapped]
    public string SpouseCitizenshipNumberMask { get; set; }
    public int BirthPlaceCode { get; set; }
    public string BirthPlace { get; set; }
    public DateTime? BirthDate { get; set; }
    public int GenderCode { get; set; }
    public string GenderDescription { get; set; }
    public int MaritalStatusCode { get; set; }
    public string MaritalStatusDescription { get; set; }
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; }
    public int RegistrationPlaceFamilyRowNumber { get; set; }
    public int RegistrationPlacePersonalRowNumber { get; set; }

    /// <summary>
    /// Il kodu
    /// </summary>
    public int CityCode { get; set; }

    /// <summary>
    /// Il adi
    /// </summary>
    public string CityName { get; set; }

    /// <summary>
    /// Ilce kodu
    /// </summary>
    public int TownCode { get; set; }

    /// <summary>
    /// Ilce adi
    /// </summary>
    public string TownName { get; set; }

    public int VolumeCode { get; set; }
    public string VolumeDescription { get; set; }
    public string SerialNumber { get; set; }
    public int RowNumber { get; set; }
    public string IdentificationCardSerialNumber { get; set; }
    public DateTime? DeathDate { get; set; }
    public string GivingAuthority { get; set; }
    public int IdentificationCertificateGivingReasonCode { get; set; }
    public string IdentificationCertificateGivingReasonDescription { get; set; }
    public int GivingReasonCode { get; set; }
    public string GivingReasonDescription { get; set; }
    public int GivingTownCode { get; set; }
    public string GivingTownName { get; set; }
    public long RecordNumber { get; set; }
    public long IdentificationCertificateRecordNumber { get; set; }
    public long IdentificationCardRecordNumber { get; set; }
    public string PreviousSurname { get; set; }
    public int ReligionCode { get; set; }
    public string ReligionDescription { get; set; }
    public int ApplicationReasonCode { get; set; }
    public string ApplicationReasonDescription { get; set; }
    public int DelivererOfficeCode { get; set; }
    public string DelivererOfficeDescription { get; set; }
    public int IssuerTownCode { get; set; }
    public string IssuerTownName { get; set; }
    public int UnclearEndDateReasonCode { get; set; }
    public string UnclearEndDateReasonDescription { get; set; }
    public int PermitterCityCode { get; set; }
    public string PermitterCityName { get; set; }
    public int SourceOfficeCode { get; set; }
    public string SourceOfficeDescription { get; set; }
    public int NationalityCode { get; set; }
    public string NationalityName { get; set; }
    public int CountryCode { get; set; }
    public string CountryName { get; set; }
    public DateTime? ValidTillDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? PermissionStartDate { get; set; }
    public DateTime? PermissionEndDate { get; set; }
    public string PermissionNumber { get; set; }
    public string DocumentNumber { get; set; }
    public int Office { get; set; }
    public long ReceivedCitizenshipNumber { get; set; }
    public long RealPersonCitizenshipNumber { get; set; }
    //  public VerifiedVia? VerifiedVia { get; set; }
    public string KpsErrorCode { get; set; }
    public string KpsErrorDescription { get; set; }
}
