using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class RemittanceTransactionModel
    { 
        public string? ControlNumber { get; set; }
        public string? DeductionCode { get; set; }
        public double? DeductionAmount { get; set; }
        public string? SCNumber { get; set; }
        public string? FullName { get; set; }
        public string? AFSN { get; set; }
        public decimal? BR { get; set; }
        public string? PN { get; set; }
        public double? SurBalAmt { get; set; }
        public double? SurBal { get; set; }
        public double? IntBalAmt { get; set; }
        public double? IntBal { get; set; }
        public double? PrnAmt { get; set; }
        public double? Balance { get; set; }
        public double? Savings { get; set; }
        public int? ACDIType { get; set; }
    }
}
