using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class CustomerEntity
    {
        #region VariableDeclarations
        private string _customerName { get; set; }
        private string _customerNumber { get; set; }
        private string _businessLine { get; set; }
        private string _TCKN { get; set; }
 
        #endregion
        #region Properties
        public string CustomerName
        {
            get { return _customerName; }
            set { _customerName = value; }
        }
        
        public string CustomerNumber
        {
            get { return _customerNumber; }
            set { _customerNumber = value; }
        }
        public string BusinessLine
        {
            get { return _businessLine; }
            set { _businessLine = value; }
        }
        public string TCKN
        {
            get { return _TCKN; }
            set { _TCKN = value; }
        }
       
        
        #endregion
    }
}