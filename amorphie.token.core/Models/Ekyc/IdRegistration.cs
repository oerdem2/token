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

     public IdRegistration(KpsIdentity kpsIdentity)
     {
         FatherName = kpsIdentity?.FatherName;
         MotherName = kpsIdentity?.MotherName;
         RegistrationPlace = kpsIdentity?.CityName;
         RegistrationPlaceFamilyRow = kpsIdentity?.RegistrationPlaceFamilyRowNumber.ToString();
         RegistrationPlacePersonalRow = kpsIdentity?.RegistrationPlacePersonalRowNumber.ToString();
         SerialNo = kpsIdentity?.IdentificationCardSerialNumber;
         RecordNo = kpsIdentity?.RecordNumber.ToString();
         IdentityType = IdRegistration.GetIdentityType(kpsIdentity?.CertificationType);
         IdentityNo = kpsIdentity?.CitizenshipNumber.ToString();
         DocumentNo = kpsIdentity?.IdentificationCardSerialNumber.ToString();
         Name = kpsIdentity?.Name;
         Surname = kpsIdentity?.Surname;
         Gender = kpsIdentity?.GenderDescription;
         BirthDate = kpsIdentity?.BirthDate != null ? kpsIdentity.BirthDate.Value.ToString("dd.MM.yyyy") : "";
         BirthPlace = kpsIdentity?.BirthPlace;
         Nationality = "TR";
         IssuedBy = kpsIdentity?.GivingAuthority;
         IssuedDate = kpsIdentity?.IssueDate != null ? kpsIdentity.IssueDate.Value.ToString("dd.MM.yyyy") : "";
         ExpireDate = kpsIdentity?.ValidTillDate != null ? kpsIdentity.ValidTillDate.Value.ToString("dd.MM.yyyy") : "";
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