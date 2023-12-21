using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Profile
{
    public class SimpleProfileResponse
    {
        public Data? data { get; set; }
        public Meta? meta { get; set; }
    }

    public class Data
    {
        public Profile? profile { get; set; }
        public List<SimpleProfilePhone>? phones { get; set; }
        public List<Relation>? relations { get; set; }
        public List<SimpleProfileEmail>? emails { get; set; }
    }

    public class SimpleProfileEmail
    {
        public string? type { get; set; }
        public string? address { get; set; }
        public bool isVerified { get; set; }
    }

    public class Meta
    {
        public string? transactionId { get; set; }
    }

    public class SimpleProfilePhone
    {
        public string? countryCode { get; set; }
        public string? number { get; set; }
        public string? prefix { get; set; }
        public string? type { get; set; }
    }

    public class Profile
    {
        public string? type { get; set; }
        public string? status { get; set; }
        public bool isBankPersonel { get; set; }
        public string? identificationNumber { get; set; }
        public string? citizenshipNumber { get; set; }
        public string? taxNo { get; set; }
        public int externalClientNo { get; set; }
        public string? businessLine { get; set; }
        public string? name { get; set; }
        public string? middleName { get; set; }
        public string? surname { get; set; }
        public bool hasMobApproval { get; set; }
        public bool hasCollectionRestriction { get; set; }
        public bool isPrivateBanking { get; set; }
        public DateTime birthDate { get; set; }
        public string? gender { get; set; }
        public string? coreBankingServiceAgreement { get; set; }
        public DateTime retiredSalaryAccCheckDate { get; set; }
        public bool hasAmlApproval { get; set; }
        public bool hasForeignIdentity { get; set; }
        public string? userName { get; set; }
        public string? companyId { get; set; }
        public int? applicationBranchCode { get; set; }
    }

    public class Relation
    {
        public int relationTypeCode { get; set; }
        public string? relationTypeName { get; set; }
        public string? relatedCustomerShortName { get; set; }
        public long relatedCustomerId { get; set; }
        public long relatedCustomerNo { get; set; }
        public double share { get; set; }
    }


}