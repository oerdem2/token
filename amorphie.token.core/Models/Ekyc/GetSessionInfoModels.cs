namespace amorphie.token.core;

public class GetSessionInfoModels
{


    public class Request
    {
        public string SessionUId { get; set; }
        public bool LoadDetails { get; set; }
        public bool LoadContent { get; set; }
        public string RoomId { get; set; }
    }

    public class Response : EkycResponseBase
    {
        public Data Data { get; set; }
    }
    public class Data
    {
        public Guid UId { get; set; }
        public string Type { get; set; }
        public DateTime DateTime { get; set; }
        public Guid DomainUId { get; set; }
        public string RoomId { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNo { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool NFCExists { get; set; }
        public bool NFCVerified { get; set; }
        public string Status { get; set; }
        // public DateTime StartDate { get; set; }
        // public DateTime LiveDate { get; set; }
        // public DateTime FinishDate { get; set; }
        // public DateTime CloseDate { get; set; }
        public Guid FaceUId { get; set; }
        public Face Face { get; set; }
        public Guid IDDocUId { get; set; }
        public IDDoc IDDoc { get; set; }
        public Guid IDChipUId { get; set; }
        public IDChip IDChip { get; set; }
        public List<DocumentBase> CallDocuments { get; set; }
    }

    public class Face : BaseDocDataClass
    {
        public string Distance { get; set; }
        public string Confidence { get; set; }
        public string ChipDistance { get; set; }
        public string ChipConfidence { get; set; }
        public int ValidityLevel { get; set; }
        public Guid FaceDocumentUId { get; set; }
        public DocumentBase FaceDocument { get; set; }
    }

    public class IDDoc : BaseDocDataClass
    {
        public Guid FrontDocumentUId { get; set; }
        public Guid BackDocumentUId { get; set; }
        public Guid HoloDocumentUId { get; set; }
        public Guid FaceDocumentUId { get; set; }
        public DocumentBase FrontDocument { get; set; }
        public DocumentBase BackDocument { get; set; }
        public DocumentBase HoloDocument { get; set; }
        public DocumentBase FaceDocument { get; set; }
    }

    public class IDChip : BaseDocDataClass
    {
        public string FaceDocumentContent { get; set; }
        public Guid FaceDocumentUId { get; set; }
        public DocumentBase FaceDocument { get; set; }
    }

    public class BaseDocDataClass
    {
        public Guid UId { get; set; }
        public Guid SessionUId { get; set; }
        public DateTime DateTime { get; set; }
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
        public bool? IsValid { get; set; }
    }

}
