using amorphie.token.core.Models.Profile;

namespace amorphie.token.core;

 public class IdRegistration
{
    public string FatherName { get; set; }
    public string MotherName { get; set; }
    public string BirthPlace { get; set; }
    public string RegistrationPlace { get; set; }
    public string RegistrationPlaceFamilyRow { get; set; }
    public string RegistrationPlacePersonalRow { get; set; }
    public string SerialNo { get; set; }
    public string RecordNo { get; set; }
    public string IdentityType { get; set; }
    public string IdentityNo { get; set; }
    public string DocumentNo { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Gender { get; set; }
    public string BirthDate { get; set; }
    public string Nationality { get; set; }
    public string IssuedBy { get; set; }
    public string IssuedDate { get; set; }
    public string ExpireDate { get; set; }
    public static string GetIdentityType(KpsCertificationType? kpsCertificationType)
    {
        switch (kpsCertificationType)
        {
            case KpsCertificationType.NewCertificate:
                return "TC Kimlik Kartı";
            case KpsCertificationType.OldCertificate:
                return "TC Kimlik Kartı";
            case KpsCertificationType.ForeignCertificate:
                return "Yabancı Kimlik Kartı";
            case KpsCertificationType.TemporaryCertificate:
                return "Geçici Kimlik Kartı";
            case KpsCertificationType.BlueCard:
                return "Mavi Kartı";
            case KpsCertificationType.NewBlueCard:
                return "Yeni Mavi Kart";
            case KpsCertificationType.TemporaryBlueCard:
                return "Yeni Mavi Kart Geçici Kimlik";
            default:
                return "Bilinmiyor";
        }
    }

    //       public static string GetIdentityType(KpsCertificationType? kpsCertificationType)
    //  {
    //      switch (kpsCertificationType)
    //      {
    //          case KpsCertificationType.NewCertificate:
    //              return "TC Kimlik Kartı";
    //          case KpsCertificationType.OldCertificate:
    //              return "TC Kimlik Kartı";
    //          case KpsCertificationType.ForeignCertificate:
    //              return "Yabancı Kimlik Kartı";
    //          case KpsCertificationType.TemporaryCertificate:
    //              return "Geçici Kimlik Kartı";
    //          case KpsCertificationType.BlueCard:
    //              return "Mavi Kartı";
    //          case KpsCertificationType.NewBlueCard:
    //              return "Yeni Mavi Kart";
    //          case KpsCertificationType.TemporaryBlueCard:
    //              return "Yeni Mavi Kart Geçici Kimlik";
    //          default:
    //              return "Bilinmiyor";
    //      }
    //  }

     public IdRegistration(ProfileResponse profile)
     {
        var address = profile.addresses.FirstOrDefault();
         FatherName = profile.fatherName;
         MotherName = profile.motherName;
         RegistrationPlace = address?.cityName.ToString();
        //  RegistrationPlaceFamilyRow = profile. //profile?.RegistrationPlaceFamilyRowNumber.ToString();
        //  RegistrationPlacePersonalRow = profile?.RegistrationPlacePersonalRowNumber.ToString();
        //  SerialNo = profile.identitySerialNo;
        //  RecordNo = profile.identitySeries;
        //  IdentityType = profile.identityType;
        //  IdentityNo = profile.identityNo;
        //  DocumentNo = profile.identitySerialNo.ToString();
         Name = profile?.customerName;
         Surname = profile?.surname;
         Gender = profile?.gender;
         BirthDate = profile?.birthDate != null ? profile.birthDate.ToString("dd.MM.yyyy") : "";
         BirthPlace = profile?.birthPlace;
         Nationality = "TR";
        //  IssuedBy = profile?.representativeCode;
        //  IssuedDate =  "";
        //  ExpireDate = "";
     }


}

public enum KpsCertificationType
{
    /// <summary>
    /// Eski kimlik
    /// </summary>
    OldCertificate = 0,

    /// <summary>
    /// Yeni kimlik
    /// </summary>
    NewCertificate = 1,

    /// <summary>
    /// Yabanci kimlik
    /// </summary>
    ForeignCertificate = 2,

    /// <summary>
    /// Gecici kimlik
    /// </summary>
    TemporaryCertificate = 3,

    /// <summary>
    /// Mavi kart
    /// </summary>
    BlueCard = 4,

    /// <summary>
    /// Yeni Mavi kart
    /// </summary>
    NewBlueCard = 5,

    /// <summary>
    /// Yeni Mavi kart Geçici Kimlik
    /// </summary>
    TemporaryBlueCard = 6
}