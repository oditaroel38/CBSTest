using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace CBS.Models
{
    public class ReportLoanLedgerDataModel
    {
        public string? Scno { get; set; }
        public string? Branch { get; set; }
        public string? Pn { get; set; }
        public string? FullName { get; set; }
        public string? DedType { get; set; }
        public decimal? SurchargeBalance { get; set; }
        public decimal? SurchargeAmount { get; set; }
        public decimal? InterestBalance { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? PrnAmount { get; set; }
        public decimal? Balance { get; set; }
        public decimal? TransactedAmount { get; set; }
        public DateTime? TransactedDate { get; set; }
        public Int64? TotalRowCount { get; set; }
        
    }
}
