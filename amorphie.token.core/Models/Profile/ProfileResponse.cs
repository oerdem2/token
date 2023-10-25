using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Profile
{
    public class ProfileResponse
    {
        public List<Address> addresses { get; set; }
        public string bankStatementAddressType { get; set; }
        public DateTime birthDate { get; set; }
        public string birthPlace { get; set; }
        public string businessLine { get; set; }
        public string cardShippingAddressType { get; set; }
        public string citizenshipNumber { get; set; }
        public string collectionStage { get; set; }
        public string coreBankingServiceAgreement { get; set; }
        public string coreBankingServiceAgreementNo { get; set; }
        public Corroboration corroboration { get; set; }
        public string customerName { get; set; }
        public string drivingLicenceNo { get; set; }
        public string educationStatus { get; set; }
        public List<Email> emails { get; set; }
        public string externalClientNo { get; set; }
        public string fatherName { get; set; }
        public string gender { get; set; }
        public int handicapRate { get; set; }
        public string handicapType { get; set; }
        public bool hasAmlApproval { get; set; }
        public bool hasCollectionRestriction { get; set; }
        public bool hasDrivingLicence { get; set; }
        public bool hasForeignIdentity { get; set; }
        public bool hasMobApproval { get; set; }
        public string identityNo { get; set; }
        public string identitySerialNo { get; set; }
        public string identitySeries { get; set; }
        public string identityType { get; set; }
        public bool isBankPersonel { get; set; }
        public bool isGrupPersonel { get; set; }
        public bool isHandicap { get; set; }
        public bool isPrivateBanking { get; set; }
        public bool isRemoteCustomer { get; set; }
        public bool isRetiredCustomer { get; set; }
        public string mainBranchCode { get; set; }
        public string maritalStatus { get; set; }
        public string mgmReferenceNo { get; set; }
        public string middleName { get; set; }
        public string motherName { get; set; }
        public string passportNo { get; set; }
        public List<Phone> phones { get; set; }
        public string portfolioCode { get; set; }
        public string privateBankingPortfolioCode { get; set; }
        public int registeredCityCode { get; set; }
        public string registeredTownName { get; set; }
        public List<object> relations { get; set; }
        public string representativeCode { get; set; }
        public string representativeName { get; set; }
        public object retiredSalaryAccCheckDate { get; set; }
        public string segmentCode { get; set; }
        public object sicilNo { get; set; }
        public string status { get; set; }
        public string subSegmentCode { get; set; }
        public string surname { get; set; }
        public string taxNo { get; set; }
        public string taxOfficeCode { get; set; }
        public string taxOfficeName { get; set; }
        public string temporaryCertificateDocumentNumber { get; set; }
        public string type { get; set; }
        public object userName { get; set; }
        public Work work { get; set; }
    }

    public class Address
    {
        public string addressDetail { get; set; }
        public int cityCode { get; set; }
        public object cityName { get; set; }
        public string companyName { get; set; }
        public int countryCode { get; set; }
        public string districtName { get; set; }
        public string fullAddressInfo { get; set; }
        public bool isBankStatementAddress { get; set; }
        public bool isCardShippingAddress { get; set; }
        public string streetName { get; set; }
        public int townCode { get; set; }
        public string townName { get; set; }
        public string type { get; set; }
        public string zipCode { get; set; }
    }

    public class Corroboration
    {
        public bool addressADNKSCheck { get; set; }
        public bool addressCheck { get; set; }
        public bool authorizedSignaturesCheck { get; set; }
        public bool casebookCheck { get; set; }
        public bool corroboratorsAuthorizationCheck { get; set; }
        public bool corroboratorsCheck { get; set; }
        public bool eMailCheck { get; set; }
        public string footballTeam { get; set; }
        public bool identityCheck { get; set; }
        public bool registerRecordCheck { get; set; }
        public bool telephoneCheck { get; set; }
    }

    public class Email
    {
        public string address { get; set; }
        public bool isVerified { get; set; }
        public string type { get; set; }
    }

    public class Phone
    {
        public string countryCode { get; set; }
        public string number { get; set; }
        public string prefix { get; set; }
        public string type { get; set; }
    }

    public class Work
    {
        public int areaCode { get; set; }
        public string areaName { get; set; }
        public int mainSectorCode { get; set; }
        public string mainSectorName { get; set; }
        public int sectorActivityCode { get; set; }
        public string sectorActivityName { get; set; }
        public int sectorCode { get; set; }
        public string sectorName { get; set; }
        public int subSectorCode { get; set; }
        public string subSectorName { get; set; }
        public string title { get; set; }
        public int titleCode { get; set; }
    }
}