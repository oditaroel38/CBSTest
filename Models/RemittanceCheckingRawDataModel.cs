using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class RemittanceCheckingRawDataModel
    {
        public string? CONTROLN { get; set; }
        public string? DEDCODE { get; set; }
        public double? DEDAMOUNT { get; set; }
        public string? SCNO { get; set; }
        public string? FullName { get; set; }
        public string? AFSN { get; set; }
        public string? BR { get; set; }
  
    }
}
