using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Consent
{
    public class DocumentResponse
    {
        public bool isAuthorized { get; set; }
        public List<ContractDocument> contractDocuments { get; set; }
    }

    public class ContractDocument
    {
        public string fileType { get; set; }
        public string fileContextType { get; set; }
        public string fileName { get; set; }
        public string documentCode { get; set; }
        public string documentVersion { get; set; }
        public string reference { get; set; }
        public string owner { get; set; }
        public string fileContext { get; set; }
        public string filePath { get; set; }
    }
}