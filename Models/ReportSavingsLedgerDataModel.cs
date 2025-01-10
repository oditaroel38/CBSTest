using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class ReportSavingsLedgerDataModel
    {
        public string? Scno { get; set; }
        public string? Branch { get; set; }
        public string? FullName { get; set; }
        public decimal? TransactedAmount { get; set; }
        public decimal? RemainingBalance { get; set; }
        public decimal? WithdrawableBalance { get; set; }
        public DateTime? TransactedDate { get; set; }
        public Int64? TotalRowCount { get; set; }

    }
}
