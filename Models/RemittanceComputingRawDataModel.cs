using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class RemittanceComputingRawDataModel
    {
        public string? CONTROLN { get; set; }
        public string? DEDCODE { get; set; }
        public double? DEDAMOUNT { get; set; }
        public double? TOTALAMT { get; set; }
        public double? CAAMT { get; set; }
        public string? SCNO { get; set; }
        public string? FullName { get; set; }
        public string? AFSN { get; set; }
        public string? BR { get; set; }
        public string? PN { get; set; }
        public double? SURBALAMT { get; set; }
        public double? SURBAL { get; set; }
        public double? INTBALAMT { get; set; }
        public double? INTBAL { get; set; }
        public double? PRNAMT { get; set; }
        public DateTime? DATEG { get; set; }
        public double? BAL { get; set; }
        public double? SAVINGS { get; set; }
        public int? ACDIType { get; set; }
    }
}
